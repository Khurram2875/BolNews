using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace CmsInstaller;

/// <summary>Carries out the installation, one clearly numbered step at a time.</summary>
public sealed class Installer
{
    private readonly Plan _plan;
    private readonly CancellationToken _ct;
    private readonly List<(string Title, string Status)> _results = new();
    private bool _aptUpdated;
    private string _backupSuffix = DateTime.Now.ToString("yyyyMMdd-HHmmss");

    public Installer(Plan plan, CancellationToken ct) { _plan = plan; _ct = ct; }

    private SiteNames N => _plan.Names;

    public async Task<int> RunAsync()
    {
        ChoosePort();
        var steps = new List<(string Title, bool Optional, Func<Task<StepResult>> Run)>
        {
            ("Check the website files and the .NET runtime", false, CheckRuntime),
            ("Install the MariaDB database server", false, InstallMariaDb),
            ("Create the database and its users", false, ProvisionDatabase),
            ("Create the tables and the first administrator", false, InstallSchema),
            ("Install the Apache web server", true, InstallApache),
            ("Save the secure settings file", false, WriteEnvironmentFile),
            ("Prepare folders and permissions", false, PrepareFolders),
            ("Create the Apache site", true, CreateApacheSite),
            ("Create and start the website service", true, CreateService),
        };

        for (var i = 0; i < steps.Count; i++)
        {
            var ok = await RunStep(i + 1, steps.Count, steps[i].Title, steps[i].Optional, steps[i].Run);
            if (!ok) { PrintSummary(false); return 1; }
        }

        await FinalChecks();
        PrintSummary(true);
        return _results.Any(r => r.Status.StartsWith("SKIPPED", StringComparison.Ordinal) || r.Status == "FAILED") ? 1 : 0;
    }

    private async Task<bool> RunStep(int index, int total, string title, bool optional, Func<Task<StepResult>> action)
    {
        while (true)
        {
            _ct.ThrowIfCancellationRequested();
            Ui.Section($"Step {index} of {total}: {title}");
            Shell.Log($"STEP {index}/{total} {title}");
            try
            {
                var result = await action();
                _results.Add((title, result == StepResult.Done ? "done" : "not needed"));
                return true;
            }
            catch (Exception ex) when (ex is OperationCanceledException or InputClosedException or UserQuitException) { throw; }
            catch (Exception ex)
            {
                Shell.Log("ERROR " + ex);
                Ui.Fail(ex.Message);
                if (ex is InstallException { Hint: not null } ie) Ui.Info(ie.Hint!);
                var options = optional
                    ? new[] { "Try this step again", "Skip this step and continue", "Stop the installer" }
                    : new[] { "Try this step again", "Stop the installer" };
                var choice = Ui.Choose("What would you like to do?", options);
                if (choice == 0) continue;
                if (optional && choice == 1) { _results.Add((title, "SKIPPED after an error")); return true; }
                _results.Add((title, "FAILED"));
                return false;
            }
        }
    }

    // ---- 1. runtime -------------------------------------------------------

    private async Task<StepResult> CheckRuntime()
    {
        var app = _plan.App;
        if (!File.Exists(app.DllPath)) throw new InstallException($"The file {app.DllPath} is missing.", "Copy the complete published website to that folder and try again.");

        if (app.SelfContained)
        {
            if (!File.Exists(app.ExePath)) throw new InstallException($"The program file {app.ExePath} is missing from the published website.",
                "Publish with: dotnet publish -c Release -r linux-x64 --self-contained true, then copy all files again.");
            await Shell.RunAsync("chmod", new[] { "+x", app.ExePath }, ct: _ct);
            _plan.ExecStart = app.ExePath;
            Ui.Good("The website includes its own .NET runtime. Nothing to install.");
            return StepResult.Done;
        }

        var dotnet = FindDotnet();
        if (dotnet is null || !await HasAspNetRuntime(dotnet))
        {
            Ui.Info("The ASP.NET Core 8 runtime is not installed. Trying to install it now...");
            await AptInstall("aspnetcore-runtime-8.0");
            dotnet = FindDotnet();
            if (dotnet is null || !await HasAspNetRuntime(dotnet))
                throw new InstallException("The ASP.NET Core 8 runtime could not be installed automatically.",
                    "Either publish the website self-contained (dotnet publish -r linux-x64 --self-contained true) and copy it again, " +
                    "or install the runtime from https://learn.microsoft.com/dotnet/core/install/linux, then run this installer again.");
        }
        _plan.ExecStart = $"{dotnet} {app.DllPath}";
        Ui.Good($"ASP.NET Core 8 runtime found ({dotnet}).");
        return StepResult.Done;
    }

    private static string? FindDotnet() =>
        new[] { Shell.Which("dotnet"), "/usr/lib/dotnet/dotnet", "/usr/share/dotnet/dotnet", "/opt/dotnet/dotnet", "/usr/local/share/dotnet/dotnet" }
            .FirstOrDefault(p => p is not null && File.Exists(p));

    private async Task<bool> HasAspNetRuntime(string dotnet)
    {
        if (Shell.DryRun) return true;
        var result = await Shell.RunAsync(dotnet, new[] { "--list-runtimes" }, ct: _ct, changesSystem: false);
        return result.Ok && result.Output.Contains("Microsoft.AspNetCore.App 8.", StringComparison.Ordinal);
    }

    // ---- 2. MariaDB -------------------------------------------------------

    private async Task<StepResult> InstallMariaDb()
    {
        if (!_plan.LocalDb) { Ui.Info("You chose another database server, so nothing is installed here."); return StepResult.Skipped; }

        if (Shell.Which("mariadbd") is not null || Shell.Which("mysqld") is not null)
            Ui.Good("A MariaDB / MySQL server is already installed. It will be used as it is.");
        else
        {
            Ui.Info("MariaDB is not installed. Installing it now; this can take a few minutes...");
            await AptInstall("mariadb-server");
            Ui.Good("MariaDB installed.");
        }

        var started = false;
        foreach (var service in new[] { "mariadb", "mysql", "mysqld" })
        {
            var r = await Shell.RunAsync("systemctl", new[] { "enable", "--now", service }, ct: _ct);
            if (r.Ok) { started = true; break; }
        }
        if (!started) throw new InstallException("The database server could not be started.", "Look at the reason with: sudo journalctl -u mariadb -n 50");

        await WaitForDatabase();
        Ui.Good("The database server is running.");
        return StepResult.Done;
    }

    private async Task WaitForDatabase()
    {
        if (Shell.DryRun) return;
        for (var i = 0; i < 40; i++)
        {
            var r = await Cli("SELECT 1");
            if (r.Ok || r.Output.Contains("Access denied", StringComparison.OrdinalIgnoreCase)) return;
            await Task.Delay(1000, _ct);
        }
        throw new InstallException("The database server did not become ready within 40 seconds.", "Look at the reason with: sudo journalctl -u mariadb -n 50");
    }

    private async Task<ShellResult> Cli(string sql)
    {
        var client = Shell.Which("mariadb") ?? Shell.Which("mysql");
        if (client is null) throw new InstallException("The MariaDB command-line client was not found.", "Install it with: sudo apt-get install mariadb-client");
        var args = new List<string> { "--protocol=socket", "-N", "-B" };
        if (!string.IsNullOrEmpty(_plan.LocalAdminUser)) { args.Add("-u"); args.Add(_plan.LocalAdminUser!); }
        var env = string.IsNullOrEmpty(_plan.LocalAdminPassword) ? null : new Dictionary<string, string> { ["MYSQL_PWD"] = _plan.LocalAdminPassword! };
        return await Shell.RunAsync(client, args, stdin: sql + "\n", env: env, timeoutSeconds: 120, ct: _ct, changesSystem: false);
    }

    // ---- 3. database + users ----------------------------------------------

    private async Task<StepResult> ProvisionDatabase()
    {
        var statements = Generators.ProvisionSql(N.Id, N.AppUser, _plan.AppDbPassword, N.MigratorUser, _plan.MigratorPassword,
            _plan.LocalDb ? new[] { "localhost", "127.0.0.1" } : new[] { _plan.RemoteUserHost });

        if (_plan.LocalDb)
        {
            if (Shell.DryRun) { Ui.Hint("[dry run] would create the database, the website user and the upgrade user."); return StepResult.Done; }

            ShellResult probe;
            for (var attempt = 0; ; attempt++)
            {
                probe = await Cli("SELECT @@port, @@skip_networking, @@socket");
                if (probe.Ok) break;
                if (!probe.Output.Contains("Access denied", StringComparison.OrdinalIgnoreCase) || attempt >= 3)
                    throw new InstallException("The installer could not log in to the database server.", Shell.Tail(probe.Output, 400));
                Ui.Warn("The database server does not let the system administrator in without a password.");
                Ui.Info("Enter the login of a database administrator (the account is used only now and is not saved).");
                _plan.LocalAdminUser = Ui.Ask("Database administrator user name", "root");
                _plan.LocalAdminPassword = Ui.AskSecret("Database administrator password");
            }

            var parts = probe.Output.Trim().Split('\t');
            var port = parts.Length > 0 && int.TryParse(parts[0], out var p) ? p : 3306;
            var skipNetworking = parts.Length > 1 && (parts[1] == "1" || parts[1].Equals("ON", StringComparison.OrdinalIgnoreCase));
            var socket = parts.Length > 2 ? parts[2].Trim() : string.Empty;
            if (skipNetworking && socket.StartsWith('/')) { _plan.DbHost = socket; Ui.Info("Database networking is switched off, so the website will use the local socket."); }
            else _plan.DbHost = "127.0.0.1";
            _plan.DbPort = port;

            var script = string.Join(";\n", statements) + ";";
            var result = await Cli(script);
            if (!result.Ok) throw new InstallException("Creating the database and users failed.", Shell.Tail(result.Output, 600));
        }
        else
        {
            if (Shell.DryRun) { Ui.Hint("[dry run] would create the database and users on the remote server."); return StepResult.Done; }
            await DatabaseSetup.RunSqlAsync(DatabaseSetup.AdminConnection(_plan.RemoteHost, _plan.RemotePort, _plan.RemoteAdminUser, _plan.RemoteAdminPassword), statements);
        }

        var appProblem = await DatabaseSetup.TestConnectionAsync(AppConnection());
        var migratorProblem = await DatabaseSetup.TestConnectionAsync(MigratorConnection());
        if (appProblem is not null || migratorProblem is not null)
            throw new InstallException("The new database users cannot connect.", appProblem ?? migratorProblem);
        Ui.Good($"Database \"{N.Id}\" is ready, with the users \"{N.AppUser}\" (website) and \"{N.MigratorUser}\" (upgrades).");
        return StepResult.Done;
    }

    private string AppConnection() => Generators.ConnectionString(_plan.DbHost, _plan.DbPort, N.Id, N.AppUser, _plan.AppDbPassword, true);
    private string MigratorConnection() => Generators.ConnectionString(_plan.DbHost, _plan.DbPort, N.Id, N.MigratorUser, _plan.MigratorPassword, false);

    // ---- 4. schema --------------------------------------------------------

    private async Task<StepResult> InstallSchema()
    {
        if (Shell.DryRun) { Ui.Hint("[dry run] would create the tables and the administrator account."); return StepResult.Done; }
        Ui.Info("Creating the tables. This takes a moment...");
        await DatabaseSetup.InstallSchemaAsync(MigratorConnection(), AppConnection(), _plan.Language, _plan.AdminEmail, _plan.AdminPassword);
        Ui.Good($"Tables created and the administrator {_plan.AdminEmail} added.");
        return StepResult.Done;
    }

    // ---- 5. Apache --------------------------------------------------------

    private async Task<StepResult> InstallApache()
    {
        if (Shell.Which("apache2") is null)
        {
            Ui.Info("Apache is not installed. Installing it now...");
            await AptInstall("apache2");
            Ui.Good("Apache installed.");
        }
        else Ui.Good("Apache is already installed.");

        var modules = new List<string> { "proxy", "proxy_http", "headers" };
        if (_plan.Https != HttpsMode.None) modules.Add("ssl");
        var enable = await Shell.RunAsync("a2enmod", modules, ct: _ct);
        if (!enable.Ok) throw new InstallException("Apache modules could not be enabled.", Shell.Tail(enable.Output, 500));

        var start = await Shell.RunAsync("systemctl", new[] { "enable", "--now", "apache2" }, ct: _ct);
        if (!start.Ok) throw new InstallException("Apache could not be started.",
            "Another web server may already be using port 80 (for example nginx). See: sudo journalctl -u apache2 -n 30");
        Ui.Good("Apache is running with the modules the website needs.");
        return StepResult.Done;
    }

    // ---- 6. environment file ----------------------------------------------

    private async Task<StepResult> WriteEnvironmentFile()
    {
        var entries = new List<EnvEntry>
        {
            new("ConnectionStrings__DefaultConnection", AppConnection(), "Database connection for the website"),
            new("DataProtection__KeyPath", N.KeyDir, "Folder where the website keeps its encryption keys, so logins and saved keys survive restarts"),
            new("DataProtection__ApplicationName", N.Id),
            new("WordPressMedia__LocalRoot", _plan.App.Directory.TrimEnd('/') + "/wwwroot/wordpress", "Folder for imported WordPress media"),
            new("Email__PublicBaseUrl", _plan.PublicUrl, "Public address of the website (used in emails and links)"),
        };

        var first = true;
        foreach (var (key, label, _) in Questions.ApiKeys)
        {
            _plan.ApiKeys.TryGetValue(key, out var value);
            entries.Add(new EnvEntry(key, string.IsNullOrEmpty(value) ? null : value, first ? "API keys (optional). Add or change a key here, then restart the service." : string.Empty));
            first = false;
        }

        if (_plan.Email.Count > 0)
        {
            var firstEmail = true;
            foreach (var pair in _plan.Email)
            {
                entries.Add(new EnvEntry(pair.Key, pair.Value, firstEmail ? "Outgoing email (SMTP)" : string.Empty));
                firstEmail = false;
            }
        }

        var content = Generators.BuildEnvFile(N, entries);
        await FileWriter.WriteSecretAsync(N.EnvFile, content, N.ConfigDir, _ct);

        var migrator = Generators.BuildEnvFile(N, new[]
        {
            new EnvEntry("ConnectionStrings__MigrationConnection", MigratorConnection(), "Used only by the administrator when upgrading the database. The website itself never reads this file.")
        });
        await FileWriter.WriteSecretAsync(N.MigratorEnvFile, migrator, N.ConfigDir, _ct);

        Ui.Good($"Settings saved to {N.EnvFile} (readable by the administrator only).");
        var saved = _plan.ApiKeys.Count(k => !string.IsNullOrEmpty(k.Value));
        Ui.Info($"{saved} of {_plan.ApiKeys.Count} API keys saved; the others are left as commented placeholders in that file.");
        return StepResult.Done;
    }

    // ---- 7. folders and permissions ---------------------------------------

    private async Task<StepResult> PrepareFolders()
    {
        var id = await Shell.RunAsync("id", new[] { "-u", _plan.ServiceUser }, ct: _ct, changesSystem: false);
        if (!id.Ok && !Shell.DryRun)
        {
            var add = await Shell.RunAsync("useradd", new[] { "--system", "--no-create-home", "--shell", "/usr/sbin/nologin", _plan.ServiceUser }, ct: _ct);
            if (!add.Ok) throw new InstallException($"The service account \"{_plan.ServiceUser}\" could not be created.", Shell.Tail(add.Output, 400));
        }

        FileWriter.EnsureDirectory(N.StateDir);
        FileWriter.EnsureDirectory(N.KeyDir);
        await Shell.RunAsync("chown", new[] { "-R", $"{_plan.ServiceUser}:{_plan.ServiceUser}", N.StateDir }, ct: _ct);
        await Shell.RunAsync("chmod", new[] { "700", N.KeyDir }, ct: _ct);

        var webRoot = Path.Combine(_plan.App.Directory, "wwwroot");
        foreach (var folder in new[] { "uploads", "images", "wordpress" })
        {
            var path = Path.Combine(webRoot, folder);
            FileWriter.EnsureDirectory(path);
            await Shell.RunAsync("chown", new[] { "-R", $"{_plan.ServiceUser}:{_plan.ServiceUser}", path }, ct: _ct);
        }

        // The website account must be able to read the program files.
        var readable = Shell.DryRun || (await Shell.RunAsync("runuser", new[] { "-u", _plan.ServiceUser, "--", "test", "-r", _plan.App.DllPath }, ct: _ct, changesSystem: false)).Ok;
        if (!readable)
        {
            Ui.Warn($"The account \"{_plan.ServiceUser}\" cannot read the website files. Making them readable (not writable)...");
            await Shell.RunAsync("chmod", new[] { "-R", "o+rX", _plan.App.Directory }, ct: _ct);
        }
        Ui.Good("Folders and permissions are ready.");
        return StepResult.Done;
    }

    // ---- 8. Apache site ---------------------------------------------------

    private async Task<StepResult> CreateApacheSite()
    {
        if (_plan.Https == HttpsMode.SelfSigned && !File.Exists(N.SelfSignedCert))
        {
            Ui.Info("Creating a self-signed certificate (valid for 10 years)...");
            FileWriter.EnsureDirectory(N.TlsDir, "700");
            var cert = await Shell.RunAsync("openssl", new[]
            {
                "req", "-x509", "-nodes", "-newkey", "rsa:2048", "-days", "3650", "-keyout", N.SelfSignedKey, "-out", N.SelfSignedCert,
                "-subj", "/CN=" + _plan.Domain
            }, ct: _ct);
            if (!cert.Ok) throw new InstallException("The self-signed certificate could not be created.", "Install openssl (sudo apt-get install openssl) or choose the HTTP-only option. " + Shell.Tail(cert.Output, 300));
            await Shell.RunAsync("chmod", new[] { "600", N.SelfSignedKey }, ct: _ct);
        }

        var content = Generators.BuildApacheSite(N, _plan.Domain, _plan.Aliases, _plan.App.Directory, _plan.Port, _plan.Https, _plan.CertFile, _plan.KeyFile);
        string? backup = null;
        if (File.Exists(N.VhostPath))
        {
            var existing = File.ReadAllText(N.VhostPath);
            if (!existing.Contains($"installer-site: {N.Id}", StringComparison.Ordinal) &&
                !Ui.Confirm($"A different Apache file already exists at {N.VhostPath}. Replace it? (a backup copy is kept)", false))
            {
                Ui.Warn("The Apache site was not created. Choose another database name to get a different file name.");
                return StepResult.Skipped;
            }
            backup = N.VhostPath + ".backup-" + _backupSuffix;
            FileWriter.Copy(N.VhostPath, backup);
        }

        WarnAboutNameClash();
        FileWriter.WriteText(N.VhostPath, content, "644");
        var enable = await Shell.RunAsync("a2ensite", new[] { N.Id }, ct: _ct);
        var test = enable.Ok ? await Shell.RunAsync(Shell.Which("apache2ctl") ?? "apache2ctl", new[] { "configtest" }, ct: _ct) : enable;
        if (!enable.Ok || !test.Ok)
        {
            await Shell.RunAsync("a2dissite", new[] { N.Id }, ct: _ct);
            if (backup is not null) FileWriter.Copy(backup, N.VhostPath); else FileWriter.Delete(N.VhostPath);
            throw new InstallException("Apache rejected the new site, so it was removed again and your other sites are unchanged.", Shell.Tail(test.Output, 700));
        }

        var reload = await Shell.RunAsync("systemctl", new[] { "reload", "apache2" }, ct: _ct);
        if (!reload.Ok) throw new InstallException("Apache could not reload its configuration.", Shell.Tail(reload.Output, 500));
        Ui.Good($"Apache site \"{N.Id}\" created at {N.VhostPath}. Other Apache sites were not changed.");
        return StepResult.Done;
    }

    private void WarnAboutNameClash()
    {
        try
        {
            var dir = "/etc/apache2/sites-enabled";
            if (!Directory.Exists(dir)) return;
            foreach (var file in Directory.GetFiles(dir, "*.conf"))
            {
                if (Path.GetFileName(file).Equals(N.Id + ".conf", StringComparison.Ordinal)) continue;
                var text = File.ReadAllText(file);
                if (Regex.IsMatch(text, @"Server(Name|Alias)\s+[^\r\n]*\b" + Regex.Escape(_plan.Domain) + @"\b"))
                    Ui.Warn($"Another Apache site ({Path.GetFileName(file)}) already answers to {_plan.Domain}. Apache will use whichever it reads first.");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* only a courtesy check */ }
    }

    // ---- 9. service -------------------------------------------------------

    private async Task<StepResult> CreateService()
    {
        var unit = "# installer-site: " + N.Id + Environment.NewLine +
                   Generators.BuildSystemdUnit(N, _plan.App.Directory, _plan.ExecStart, _plan.Port, _plan.ServiceUser, _plan.LocalDb, Environment.ProcessorCount >= 4);
        if (File.Exists(N.ServicePath) && !File.ReadAllText(N.ServicePath).Contains($"installer-site: {N.Id}", StringComparison.Ordinal))
        {
            if (!Ui.Confirm($"A different service file already exists at {N.ServicePath}. Replace it? (a backup copy is kept)", false))
                throw new InstallException("The service was not created because a different service file already exists.", "Choose another database name to get a different service name.");
            FileWriter.Copy(N.ServicePath, N.ServicePath + ".backup-" + _backupSuffix);
        }

        FileWriter.WriteText(N.ServicePath, unit, "644");
        foreach (var args in new[] { new[] { "daemon-reload" }, new[] { "enable", N.ServiceName }, new[] { "restart", N.ServiceName } })
        {
            var r = await Shell.RunAsync("systemctl", args, ct: _ct);
            if (!r.Ok) throw new InstallException($"systemctl {args[0]} failed.", Shell.Tail(r.Output, 500));
        }

        if (Shell.DryRun) return StepResult.Done;
        Ui.Info("Waiting for the website to start...");
        var (up, status) = await WaitForWebsite($"http://127.0.0.1:{_plan.Port}/", null, 60);
        if (!up)
        {
            var log = await Shell.RunAsync("journalctl", new[] { "-u", N.ServiceName, "-n", "25", "--no-pager" }, ct: _ct, changesSystem: false);
            throw new InstallException("The website service started but did not answer within 60 seconds.", "Recent log lines:\n" + Shell.Tail(log.Output, 1800));
        }
        if (status >= 500)
            Ui.Warn($"The website answered with an error ({status}). See the log with: sudo journalctl -u {N.Id} -n 50");
        else
            Ui.Good($"The website service \"{N.Id}\" is running and answering.");
        return StepResult.Done;
    }

    private async Task<(bool Up, int Status)> WaitForWebsite(string url, string? hostHeader, int seconds)
    {
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(8) };
        for (var i = 0; i < seconds / 2; i++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (hostHeader is not null) request.Headers.Host = hostHeader;
                using var response = await client.SendAsync(request, _ct);
                return (true, (int)response.StatusCode);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { await Task.Delay(2000, _ct); }
        }
        return (false, 0);
    }

    private async Task FinalChecks()
    {
        Ui.Section("Final check");
        if (Shell.DryRun) { Ui.Hint("[dry run] no checks needed."); return; }
        var (up, status) = await WaitForWebsite("http://127.0.0.1/", _plan.Domain, 10);
        if (!up) Ui.Warn("Apache did not answer on port 80 for this domain. Check: sudo apache2ctl -S");
        else if (status >= 500) Ui.Warn($"Apache answered with {status} for {_plan.Domain}. The website may still be starting; try again in a minute.");
        else Ui.Good($"Apache serves {_plan.Domain} and passes requests to the website.");
    }

    // ---- helpers ----------------------------------------------------------

    private void ChoosePort()
    {
        var used = new HashSet<int>();
        int? own = null;
        try
        {
            if (File.Exists(N.ServicePath))
            {
                var m = Regex.Match(File.ReadAllText(N.ServicePath), @"ASPNETCORE_URLS=http://127\.0\.0\.1:(\d+)");
                if (m.Success) own = int.Parse(m.Groups[1].Value);
            }
            foreach (var dir in new[] { "/etc/systemd/system", "/etc/apache2/sites-available" })
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.GetFiles(dir))
                {
                    if (file.Contains($"/{N.Id}.", StringComparison.Ordinal)) continue;
                    foreach (Match m in Regex.Matches(File.ReadAllText(file), @"127\.0\.0\.1:(\d{2,5})")) used.Add(int.Parse(m.Groups[1].Value));
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* best effort */ }

        if (own is not null) { _plan.Port = own.Value; return; }
        for (var port = 5000; port < 5200; port++)
        {
            if (used.Contains(port)) continue;
            try { var l = new TcpListener(IPAddress.Loopback, port); l.Start(); l.Stop(); _plan.Port = port; return; }
            catch (SocketException) { /* busy */ }
        }
    }

    private async Task AptInstall(string package)
    {
        if (Shell.Which("apt-get") is null) throw new InstallException("This installer supports Debian and Ubuntu servers (it needs apt-get).", $"Install {package} yourself, then run the installer again.");
        if (!_aptUpdated)
        {
            Ui.Info("Updating the package list...");
            var update = await Shell.RunAsync("apt-get", new[] { "update" }, timeoutSeconds: 300, ct: _ct);
            if (!update.Ok) throw new InstallException("The package list could not be updated.", ExplainApt(update.Output));
            _aptUpdated = true;
        }
        var install = await Shell.RunAsync("apt-get", new[] { "install", "-y", package }, timeoutSeconds: 1200, ct: _ct);
        if (!install.Ok) throw new InstallException($"Installing {package} failed.", ExplainApt(install.Output));
    }

    public static string ExplainApt(string output)
    {
        if (output.Contains("Could not get lock", StringComparison.OrdinalIgnoreCase) || output.Contains("is another process using it", StringComparison.OrdinalIgnoreCase))
            return "Another program is using the package manager (often automatic updates). Wait a minute and choose \"Try this step again\".";
        if (output.Contains("Temporary failure", StringComparison.OrdinalIgnoreCase) || output.Contains("Could not resolve", StringComparison.OrdinalIgnoreCase) || output.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase))
            return "This server cannot reach the package servers. Check its internet connection and DNS, then choose \"Try this step again\".";
        return Shell.Tail(output, 600);
    }

    private void PrintSummary(bool allDone)
    {
        Ui.Title(allDone ? "Installation finished" : "Installation stopped");
        foreach (var (title, status) in _results)
        {
            var line = $"{title}: {status}";
            if (status == "FAILED" || status.StartsWith("SKIPPED", StringComparison.Ordinal)) Ui.Warn(line); else Ui.Good(line);
        }

        if (!allDone)
        {
            Ui.Line();
            Ui.Info("Nothing is lost: run the installer again and it will continue, keeping what is already set up.");
            Ui.Info($"Details are in {N.LogPath}");
            return;
        }

        Ui.Line();
        Ui.Info("What was created");
        Ui.Info($"  Website files:   {_plan.App.Directory}");
        Ui.Info($"  Database:        {N.Id}   (users {N.AppUser} and {N.MigratorUser})");
        Ui.Info($"  Settings/secrets {N.EnvFile}   (administrator only)");
        Ui.Info($"  Apache site:     {N.VhostPath}");
        Ui.Info($"  Service:         {N.ServiceName}   (port {_plan.Port}, local only)");
        Ui.Info($"  Installer log:   {N.LogPath}");
        Ui.Line();
        Ui.Info("Next steps");
        Ui.Info($"  1. Make sure the DNS record of {_plan.Domain} points to this server (or to Cloudflare).");
        Ui.Info($"  2. Open {_plan.PublicUrl}/Admin and sign in as {_plan.AdminEmail}.");
        Ui.Info($"  3. To add or change an API key later: sudo nano {N.EnvFile}  then  sudo systemctl restart {N.Id}");
        Ui.Info($"  4. Check the website any time with: sudo systemctl status {N.Id}   or   sudo journalctl -u {N.Id} -f");
    }
}

public enum StepResult { Done, Skipped }
