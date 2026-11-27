using System.Text;
using CmsInstaller;

// The installer never closes on its own: every problem is explained, logged and (where possible) can be retried.
Console.OutputEncoding = Encoding.UTF8;
var dryRun = args.Contains("--dry-run");
string? appDirArgument = null;
for (var i = 0; i < args.Length - 1; i++) if (args[i] == "--app-dir") appDirArgument = args[i + 1];

if (args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("CMS installer for Debian / Ubuntu servers");
    Console.WriteLine("  sudo ./installer                 run the guided installation");
    Console.WriteLine("  --app-dir /var/www/aspnetapp     skip the search and use this published website folder");
    Console.WriteLine("  --dry-run                        ask the questions and show what would be done, changing nothing");
    return 0;
}

using var cts = new CancellationTokenSource();
var interrupts = 0;
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    if (++interrupts == 1)
    {
        Ui.Cancelled = true;
        cts.Cancel();
        Ui.Warn("Stopping after the current action. It is safe to run the installer again. (Press Ctrl+C again to quit immediately.)");
    }
    else Environment.Exit(130);
};

var exitCode = 0;
var logPath = string.Empty;
try
{
    Shell.DryRun = dryRun;
    Ui.Title(dryRun ? "CMS installer (dry run: nothing will be changed)" : "CMS installer");
    Ui.Info("This guided installer sets up the database, web server, settings file and service for your website.");
    Ui.Info("You can press Ctrl+C at any time; running it again is always safe.");

    if (!OperatingSystem.IsLinux() && !dryRun)
    {
        Ui.Fail("This installer is for Linux servers (Debian or Ubuntu).");
        Ui.Info("You can still preview it on this computer with:  installer --dry-run");
        return 2;
    }
    if (OperatingSystem.IsLinux() && !dryRun && !Questions.IsRoot())
    {
        Ui.Fail("The installer needs administrator rights to install software and create services.");
        Ui.Info("Start it again with sudo, for example:  sudo ./installer");
        return 2;
    }
    if (OperatingSystem.IsLinux() && !dryRun && !Questions.IsDebianFamily())
    {
        Ui.Fail("This installer supports Debian and Ubuntu (it uses apt and Apache's Debian layout).");
        Ui.Info("On other systems, install MariaDB, Apache and the service by hand using docs/productization/installation.md.");
        return 2;
    }

    var plan = Questions.Collect(appDirArgument, dryRun);
    logPath = plan.Names.LogPath;
    Shell.SetLog(logPath);
    Shell.Log($"Installer started for site '{plan.Names.Id}' ({plan.Domain}), app folder {plan.App.Directory}");

    Ui.Section("Please check your answers");
    Ui.Info($"Website files:   {plan.App.Directory}");
    Ui.Info($"Database:        {plan.Names.Id} ({(plan.LocalDb ? "MariaDB on this server" : plan.RemoteHost + ":" + plan.RemotePort)})");
    Ui.Info($"Address:         {plan.Domain}{(plan.Aliases.Count > 0 ? " + " + string.Join(", ", plan.Aliases) : string.Empty)}   HTTPS: {plan.Https}");
    Ui.Info($"Language:        {(plan.Language == "ur" ? "Urdu" : "English")}");
    Ui.Info($"Administrator:   {plan.AdminEmail}");
    Ui.Info($"API keys to save: {plan.ApiKeys.Count(k => !string.IsNullOrEmpty(k.Value))} of {plan.ApiKeys.Count}");
    Ui.Info($"Will create:     {plan.Names.EnvFile}, {plan.Names.VhostPath}, {plan.Names.ServicePath}");
    if (!Ui.Confirm("Start the installation?", true)) throw new UserQuitException();

    exitCode = await new Installer(plan, cts.Token).RunAsync();
}
catch (UserQuitException)
{
    Ui.Line();
    Ui.Info("Installation was not started or was stopped at your request. Nothing further was changed.");
    exitCode = 0;
}
catch (OperationCanceledException)
{
    Ui.Line();
    Ui.Warn("Installation interrupted. Nothing is lost: run the installer again and it will continue.");
    exitCode = 130;
}
catch (InputClosedException ex)
{
    Ui.Line();
    Ui.Fail(ex.Message + " Run the installer from an interactive terminal.");
    exitCode = 1;
}
catch (Exception ex)
{
    Shell.Log("UNEXPECTED " + ex);
    Ui.Line();
    Ui.Fail("An unexpected problem stopped the installer: " + ex.Message);
    Ui.Info("Nothing is lost: you can run the installer again.");
    if (logPath.Length > 0) Ui.Info($"Technical details are in {logPath}");
    exitCode = 1;
}

Ui.Line();
Ui.PressEnter();
return exitCode;
