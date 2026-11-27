namespace CmsInstaller;

public sealed class InstallException : Exception
{
    public string? Hint { get; }
    public InstallException(string message, string? hint = null) : base(message) { Hint = hint; }
}

/// <summary>Everything the user answered, plus values the installer works out along the way.</summary>
public sealed class Plan
{
    public SiteNames Names { get; set; } = new("site");
    public AppInfo App { get; set; } = new(string.Empty, string.Empty, false);
    public bool DryRun { get; set; }

    // database
    public bool LocalDb { get; set; } = true;
    public string RemoteHost { get; set; } = "127.0.0.1";
    public int RemotePort { get; set; } = 3306;
    public string RemoteAdminUser { get; set; } = string.Empty;
    public string RemoteAdminPassword { get; set; } = string.Empty;
    public string RemoteUserHost { get; set; } = "%";
    public string? LocalAdminUser { get; set; }
    public string? LocalAdminPassword { get; set; }
    public string DbHost { get; set; } = "127.0.0.1";
    public int DbPort { get; set; } = 3306;
    public string AppDbPassword { get; set; } = Generators.Password();
    public string MigratorPassword { get; set; } = Generators.Password();

    // web
    public string Domain { get; set; } = string.Empty;
    public List<string> Aliases { get; set; } = new();
    public string PublicUrl { get; set; } = string.Empty;
    public HttpsMode Https { get; set; } = HttpsMode.None;
    public string? CertFile { get; set; }
    public string? KeyFile { get; set; }
    public int Port { get; set; } = 5000;

    // site
    public string Language { get; set; } = "en";
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;

    // keys: value null = not provided (written as a commented placeholder)
    public Dictionary<string, string?> ApiKeys { get; set; } = new();
    public Dictionary<string, string> Email { get; set; } = new();
    public Dictionary<string, string> ExistingEnv { get; set; } = new();

    // worked out while installing
    public string ExecStart { get; set; } = string.Empty;
    public string ServiceUser { get; set; } = "www-data";
}
