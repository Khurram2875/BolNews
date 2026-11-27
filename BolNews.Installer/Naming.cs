using System.Text.RegularExpressions;

namespace CmsInstaller;

/// <summary>
/// Every file, user and service the installer creates is named after the database, so several sites can live on one server
/// and nothing carries the product name. The database name is lower-case, 2 to 24 characters (so "{name}_migrator" fits the
/// 32-character user-name limit of older MySQL servers).
/// </summary>
public sealed record SiteNames(string Id)
{
    public string AppUser => $"{Id}_app";
    public string MigratorUser => $"{Id}_migrator";
    public string ConfigDir => $"/etc/{Id}";
    public string EnvFile => $"/etc/{Id}/{Id}.env";
    public string MigratorEnvFile => $"/etc/{Id}/{Id}-migrator.env";
    public string TlsDir => $"/etc/{Id}/tls";
    public string SelfSignedCert => $"/etc/{Id}/tls/{Id}.crt";
    public string SelfSignedKey => $"/etc/{Id}/tls/{Id}.key";
    public string KeyDir => $"/var/lib/{Id}/keys";
    public string StateDir => $"/var/lib/{Id}";
    public string ServiceName => $"{Id}.service";
    public string ServicePath => $"/etc/systemd/system/{Id}.service";
    public string VhostPath => $"/etc/apache2/sites-available/{Id}.conf";
    public string LogPath => $"/var/log/{Id}-installer.log";

    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "mysql", "mariadb", "sys", "information_schema", "performance_schema", "root", "apache2", "default", "test",
        "ssh", "sshd", "systemd", "www", "www_data", "nginx", "dotnet", "network", "cron"
    };

    /// <summary>Returns a friendly problem description, or null when the name is acceptable.</summary>
    public static string? ValidateDatabaseName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Please enter a name.";
        if (!Regex.IsMatch(value, "^[a-z][a-z0-9_]{1,23}$"))
            return "Use 2 to 24 characters: lower-case letters, digits and underscores, starting with a letter (for example acme_news).";
        if (Reserved.Contains(value)) return $"\"{value}\" is reserved by the server. Please choose another name.";
        return null;
    }

    public static string? ValidateDomain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Please enter a domain name.";
        if (!Regex.IsMatch(value, "^[a-zA-Z0-9]([a-zA-Z0-9.-]{0,251}[a-zA-Z0-9])?$") || value.Contains("..", StringComparison.Ordinal))
            return "Enter only the domain, such as news.example.com (no http://, no slashes, no spaces).";
        return null;
    }

    public static string? ValidateEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") ? null : "Enter a valid email address.";

    public static string? ValidatePublicUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? null : "Enter the full address starting with http:// or https://.";
}
