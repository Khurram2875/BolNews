using System.Runtime.InteropServices;

namespace CmsInstaller;

/// <summary>Asks everything up front, so the actual installation can run without interruptions.</summary>
public static class Questions
{
    public static readonly (string Key, string Label, string Help)[] ApiKeys =
    {
        ("Gemini__ApiKey", "Google Gemini API key", "Used for AI grammar and writing help in the article editor."),
        ("OpenAI__ApiKey", "ChatGPT (OpenAI) API key", "Stored for ChatGPT features; nothing in the CMS reads it yet."),
        ("RapidApi__Key", "RapidAPI key", "Used for the gold-rate widget."),
        ("WeatherApi__Key", "Weather API key", "Used for the weather widget."),
        ("ForexApi__Key", "Currency-rate (Forex) API key", "Used for the exchange-rate widget.")
    };

    [DllImport("libc", EntryPoint = "geteuid")]
    private static extern uint GetEuid();

    public static bool IsRoot() => OperatingSystem.IsLinux() && GetEuid() == 0;

    public static bool IsDebianFamily()
    {
        try
        {
            var text = File.ReadAllText("/etc/os-release").ToLowerInvariant();
            return text.Contains("id=debian") || text.Contains("id=ubuntu") || text.Contains("debian") || text.Contains("ubuntu");
        }
        catch { return false; }
    }

    public static Plan Collect(string? appDirArgument, bool dryRun)
    {
        var plan = new Plan { DryRun = dryRun };

        Ui.Section("Part 1 of 5 - Your website files");
        Ui.Info("Before the installer can continue, the published website must be copied onto this server,");
        Ui.Info("for example into /var/www/aspnetapp/ (the folder that contains the .dll file and the wwwroot folder).");
        plan.App = ChooseApp(appDirArgument);
        Ui.Good($"Website found: {plan.App.Directory}  ({plan.App.AssemblyName}.dll, {(plan.App.SelfContained ? "includes its own .NET runtime" : "needs the .NET runtime")})");

        Ui.Section("Part 2 of 5 - Database");
        Ui.Info("The database name is also used to name the settings file, the service and the Apache site,");
        Ui.Info("so choose something that identifies this website (for example acme_news).");
        var dbName = Ui.Ask("Database name", "cms_site", v => SiteNames.ValidateDatabaseName(v.ToLowerInvariant()) is { } p ? p : null).ToLowerInvariant();
        plan.Names = new SiteNames(dbName);
        try
        {
            if (File.Exists(plan.Names.EnvFile)) plan.ExistingEnv = Generators.ParseEnvFile(File.ReadAllText(plan.Names.EnvFile));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* not readable: treat as new */ }
        if (plan.ExistingEnv.Count > 0) Ui.Info($"An earlier installation of \"{dbName}\" was found. Its saved keys can be kept.");

        var dbMode = Ui.Choose("Where is the database server?", new[]
        {
            "On this server (recommended): MariaDB is installed automatically if it is missing",
            "On another server (advanced): you provide the address and an administrator login"
        });
        plan.LocalDb = dbMode == 0;
        if (!plan.LocalDb)
        {
            plan.RemoteHost = Ui.Ask("Database server address");
            plan.RemotePort = int.Parse(Ui.Ask("Database server port", "3306", v => int.TryParse(v, out var p) && p is > 0 and < 65536 ? null : "Enter a port from 1 to 65535."));
            plan.RemoteAdminUser = Ui.Ask("Database administrator user name");
            plan.RemoteAdminPassword = Ui.AskSecret("Database administrator password (used only during installation)");
            plan.RemoteUserHost = Ui.Ask("Allow the website's database users to connect from", "%", help: "% means any address; use this server's address to be stricter.");
            plan.DbHost = plan.RemoteHost;
            plan.DbPort = plan.RemotePort;
        }

        Ui.Section("Part 3 of 5 - Web address");
        plan.Domain = Ui.Ask("Domain name of the website", null, v => SiteNames.ValidateDomain(v), "For example news.example.com. Point this name's DNS record at this server.").ToLowerInvariant();
        var aliases = Ui.AskOptional("Other names for the same site, separated by spaces (press Enter for none)", "For example www.example.com");
        plan.Aliases = aliases.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.ToLowerInvariant()).ToList();
        foreach (var alias in plan.Aliases.ToList())
            if (SiteNames.ValidateDomain(alias) is { } problem) { Ui.Warn($"Ignoring \"{alias}\": {problem}"); plan.Aliases.Remove(alias); }

        var https = Ui.Choose("How should secure (HTTPS) connections work?", new[]
        {
            "HTTP only on this server. Choose this if Cloudflare or a load balancer in front handles HTTPS",
            "Use a certificate I already have (for example a Cloudflare Origin certificate)",
            "Create a self-signed certificate (works with Cloudflare 'Full' mode, or for testing)"
        });
        plan.Https = https switch { 1 => HttpsMode.OwnCertificate, 2 => HttpsMode.SelfSigned, _ => HttpsMode.None };
        if (plan.Https == HttpsMode.OwnCertificate)
        {
            plan.CertFile = Ui.Ask("Full path of the certificate file (.crt or .pem)", null, v => File.Exists(v) || plan.DryRun ? null : "That file does not exist.");
            plan.KeyFile = Ui.Ask("Full path of the private key file (.key)", null, v => File.Exists(v) || plan.DryRun ? null : "That file does not exist.");
        }
        else if (plan.Https == HttpsMode.SelfSigned)
        {
            plan.CertFile = plan.Names.SelfSignedCert;
            plan.KeyFile = plan.Names.SelfSignedKey;
        }
        plan.PublicUrl = Ui.Ask("Public web address (used in emails and links)", (plan.Https == HttpsMode.None ? "https://" : "https://") + plan.Domain,
            v => SiteNames.ValidatePublicUrl(v)).TrimEnd('/');

        Ui.Section("Part 4 of 5 - Website and administrator");
        plan.Language = Ui.Choose("Public site language", new[] { "English", "Urdu (right-to-left)" }) == 1 ? "ur" : "en";
        plan.AdminEmail = Ui.Ask("Email address of the first CMS administrator", null, v => SiteNames.ValidateEmail(v));
        while (true)
        {
            plan.AdminPassword = Ui.AskSecret("Administrator password (at least 12 characters, with upper and lower case, a digit and a symbol)", minLength: 12);
            var problem = PasswordProblem(plan.AdminPassword);
            if (problem is not null) { Ui.Warn(problem); continue; }
            if (Ui.AskSecret("Type the password again") == plan.AdminPassword) break;
            Ui.Warn("The two passwords are different. Please try again.");
        }

        Ui.Section("Part 5 of 5 - API keys (optional)");
        Ui.Info("Each key is optional and can be added later in the settings file. For every key you enter,");
        Ui.Info("you choose whether to save it. Typed characters appear as * so nobody can read them over your shoulder.");
        foreach (var (key, label, help) in ApiKeys) plan.ApiKeys[key] = AskKey(plan, key, label, help);

        Ui.Line();
        if (Ui.Confirm("Set up outgoing email (SMTP) for password resets and notifications now?", false))
        {
            plan.Email["Email__SmtpHost"] = Ui.Ask("SMTP server address", plan.ExistingEnv.GetValueOrDefault("Email__SmtpHost"));
            plan.Email["Email__SmtpPort"] = Ui.Ask("SMTP port", plan.ExistingEnv.GetValueOrDefault("Email__SmtpPort") ?? "587", v => int.TryParse(v, out var p) && p is > 0 and < 65536 ? null : "Enter a port from 1 to 65535.");
            plan.Email["Email__EnableSsl"] = Ui.Confirm("Use a secure (TLS) connection?", true) ? "true" : "false";
            var user = Ui.AskOptional("SMTP user name (press Enter if none)");
            if (user.Length > 0)
            {
                plan.Email["Email__SmtpUsername"] = user;
                var password = Ui.AskSecret("SMTP password", allowEmpty: true);
                if (password.Length > 0 && Ui.Confirm("Save the SMTP password in the settings file?", true)) plan.Email["Email__SmtpPassword"] = password;
            }
            plan.Email["Email__FromEmail"] = Ui.Ask("Send emails from this address", plan.AdminEmail, v => SiteNames.ValidateEmail(v));
            plan.Email["Email__FromName"] = Ui.Ask("Sender name", plan.Domain);
        }
        return plan;
    }

    private static string? AskKey(Plan plan, string key, string label, string help)
    {
        Ui.Line();
        Ui.Info(label);
        Ui.Hint(help);
        var existing = plan.ExistingEnv.GetValueOrDefault(key);
        var hasExisting = !string.IsNullOrEmpty(existing);
        var prompt = hasExisting
            ? "A key is already saved. Press Enter to keep it, paste a new key to replace it, or type - to remove it"
            : "Paste the key, or press Enter to skip";
        var value = Ui.AskSecret(prompt, allowEmpty: true);
        if (value.Length == 0) return hasExisting ? existing : null;
        if (value == "-") { Ui.Info("The saved key will be removed."); return null; }
        Ui.Hint($"({value.Length} characters entered)");
        if (Ui.Confirm("Save this key in the settings file?", true)) return value;
        Ui.Info("Not saved. You can add it to the settings file later.");
        return hasExisting ? existing : null;
    }

    public static string? PasswordProblem(string p)
    {
        if (p.Length < 12) return "The password must have at least 12 characters.";
        if (!p.Any(char.IsUpper)) return "The password needs an upper-case letter.";
        if (!p.Any(char.IsLower)) return "The password needs a lower-case letter.";
        if (!p.Any(char.IsDigit)) return "The password needs a digit.";
        if (p.All(char.IsLetterOrDigit)) return "The password needs a symbol such as ! @ # or -.";
        return null;
    }

    private static AppInfo ChooseApp(string? argument)
    {
        if (!string.IsNullOrWhiteSpace(argument))
        {
            var given = AppLocator.Inspect(argument);
            if (given is not null) return given;
            Ui.Warn($"No published website was found in {argument}.");
        }

        while (true)
        {
            var found = AppLocator.Scan(Array.Empty<string>());
            if (found.Count == 1)
            {
                Ui.Info($"Found a website in {found[0].Directory}");
                if (Ui.Confirm("Install for this website?", true)) return found[0];
            }
            else if (found.Count > 1)
            {
                var labels = found.Select(f => f.Directory).Append("Another folder (I will type the path)").ToList();
                var pick = Ui.Choose("Several websites were found. Which one are you installing?", labels);
                if (pick < found.Count) return found[pick];
            }
            else
            {
                Ui.Warn("No published website was found in the usual places (/var/www, /opt, /srv).");
            }

            Ui.Info("Copy the published files to a folder on this server first. The folder must contain the .dll file");
            Ui.Info("(its name ends in .Web.dll), the matching .runtimeconfig.json file and the wwwroot folder.");
            var path = Ui.Ask("Folder path (type q to quit, or press Enter after copying the files to look again)", "scan");
            if (path.Equals("q", StringComparison.OrdinalIgnoreCase) || path.Equals("quit", StringComparison.OrdinalIgnoreCase)) throw new UserQuitException();
            if (path == "scan") continue;
            var info = AppLocator.Inspect(path);
            if (info is not null) return info;
            Ui.Warn($"That folder does not contain a published website ({path}).");
        }
    }
}
