using System.Security.Cryptography;
using System.Text;

namespace CmsInstaller;

public enum HttpsMode { None, OwnCertificate, SelfSigned }

/// <summary>One line of the environment file. A null value writes a commented-out placeholder the publisher can fill in later.</summary>
public sealed record EnvEntry(string Key, string? Value, string Comment = "");

/// <summary>Pure text generators (no disk, no commands) so they can be unit tested.</summary>
public static class Generators
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    /// <summary>Random password using only letters and digits, so it never needs quoting in SQL, connection strings or env files.</summary>
    public static string Password(int length = 32)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++) chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return new string(chars);
    }

    public static string EscapeSql(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "''", StringComparison.Ordinal);

    /// <summary>
    /// SQL that creates the database plus two users limited to it: the website user (data access only) and a migrator user
    /// (all rights on this one database, used for schema upgrades). Neither can touch other databases.
    /// </summary>
    public static List<string> ProvisionSql(string database, string appUser, string appPassword, string migratorUser, string migratorPassword, IEnumerable<string> hosts)
    {
        var sql = new List<string>
        {
            $"CREATE DATABASE IF NOT EXISTS `{database}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci"
        };
        foreach (var host in hosts)
        {
            var h = EscapeSql(host);
            sql.Add($"CREATE USER IF NOT EXISTS '{EscapeSql(appUser)}'@'{h}' IDENTIFIED BY '{EscapeSql(appPassword)}'");
            sql.Add($"ALTER USER '{EscapeSql(appUser)}'@'{h}' IDENTIFIED BY '{EscapeSql(appPassword)}'");
            sql.Add($"GRANT SELECT, INSERT, UPDATE, DELETE, CREATE TEMPORARY TABLES, LOCK TABLES ON `{database}`.* TO '{EscapeSql(appUser)}'@'{h}'");
            sql.Add($"CREATE USER IF NOT EXISTS '{EscapeSql(migratorUser)}'@'{h}' IDENTIFIED BY '{EscapeSql(migratorPassword)}'");
            sql.Add($"ALTER USER '{EscapeSql(migratorUser)}'@'{h}' IDENTIFIED BY '{EscapeSql(migratorPassword)}'");
            sql.Add($"GRANT ALL PRIVILEGES ON `{database}`.* TO '{EscapeSql(migratorUser)}'@'{h}'");
        }
        sql.Add("FLUSH PRIVILEGES");
        return sql;
    }

    /// <summary>MySqlConnector connection string. A host starting with "/" is a Unix socket path.</summary>
    public static string ConnectionString(string host, int port, string database, string user, string password, bool forApp)
    {
        var sb = new StringBuilder();
        if (host.StartsWith('/')) sb.Append($"Server={host};ConnectionProtocol=UnixSocket;");
        else sb.Append($"Server={host};Port={port};");
        sb.Append($"Database={database};User={user};Password={password};SslMode=Preferred;AllowUserVariables=true;");
        sb.Append(forApp
            ? "Pooling=true;MinimumPoolSize=5;MaximumPoolSize=100;ConnectionTimeout=30;DefaultCommandTimeout=60;"
            : "ConnectionTimeout=30;DefaultCommandTimeout=300;");
        return sb.ToString();
    }

    // ---- environment file -------------------------------------------------

    /// <summary>Quotes a value for a systemd EnvironmentFile line.</summary>
    public static string EnvQuote(string value)
    {
        var clean = value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
        return "\"" + clean.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    public static string BuildEnvFile(SiteNames names, IEnumerable<EnvEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Settings and secrets for the website \"{names.Id}\".");
        sb.AppendLine("# Only the administrator (root) can read this file. After changing anything here, apply it with:");
        sb.AppendLine($"#     sudo systemctl restart {names.Id}");
        sb.AppendLine("# Lines that start with # are ignored. To add a missing key, delete the # and type the key between the quotes.");
        foreach (var e in entries)
        {
            if (!string.IsNullOrEmpty(e.Comment)) { sb.AppendLine(); sb.AppendLine("# " + e.Comment); }
            if (e.Value is null) sb.AppendLine($"# {e.Key}=\"\"");
            else sb.AppendLine($"{e.Key}={EnvQuote(e.Value)}");
        }
        return sb.ToString();
    }

    /// <summary>Reads KEY="value" lines back (used to keep saved keys when the installer is run a second time).</summary>
    public static Dictionary<string, string> ParseEnvFile(string text)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            {
                var inner = value[1..^1];
                var sb = new StringBuilder();
                for (var i = 0; i < inner.Length; i++)
                {
                    if (inner[i] == '\\' && i + 1 < inner.Length) { sb.Append(inner[i + 1]); i++; }
                    else sb.Append(inner[i]);
                }
                value = sb.ToString();
            }
            result[key] = value;
        }
        return result;
    }

    // ---- Apache -----------------------------------------------------------

    public static string BuildApacheSite(SiteNames names, string serverName, IEnumerable<string> aliases, string appDir, int port,
        HttpsMode https, string? certFile, string? keyFile)
    {
        var webRoot = appDir.TrimEnd('/') + "/wwwroot";
        var aliasLine = aliases.Any() ? $"    ServerAlias {string.Join(' ', aliases)}\n" : string.Empty;

        string Body(bool secure) =>
            $"    ServerName {serverName}\n" + aliasLine +
            (secure ? $"    SSLEngine on\n    SSLCertificateFile {certFile}\n    SSLCertificateKeyFile {keyFile}\n    RequestHeader set X-Forwarded-Proto \"https\"\n\n" : "\n") +
            "    ProxyPreserveHost On\n\n" +
            "    # Static files are served by Apache straight from the website's wwwroot folder.\n" +
            $"    DocumentRoot {webRoot}\n" +
            $"    <Directory {webRoot}>\n" +
            "        Options FollowSymLinks\n" +
            "        AllowOverride None\n" +
            "        Require all granted\n" +
            "    </Directory>\n\n" +
            "    ProxyPass /css !\n    ProxyPass /js !\n    ProxyPass /images !\n    ProxyPass /lib !\n    ProxyPass /uploads !\n\n" +
            "    # Everything else goes to the website application.\n" +
            $"    ProxyPass / http://127.0.0.1:{port}/ connectiontimeout=5 timeout=120 keepalive=On\n" +
            $"    ProxyPassReverse / http://127.0.0.1:{port}/\n\n" +
            $"    ErrorLog ${{APACHE_LOG_DIR}}/{names.Id}-{(secure ? "ssl-" : string.Empty)}error.log\n" +
            $"    CustomLog ${{APACHE_LOG_DIR}}/{names.Id}-{(secure ? "ssl-" : string.Empty)}access.log combined\n";

        var sb = new StringBuilder();
        sb.AppendLine($"# Created by the installer for the website \"{names.Id}\" (installer-site: {names.Id}).");
        sb.AppendLine("# You may edit this file. Other Apache sites are never touched by the installer.");
        sb.AppendLine("<VirtualHost *:80>");
        sb.Append(Body(false));
        sb.AppendLine("</VirtualHost>");
        if (https != HttpsMode.None)
        {
            sb.AppendLine();
            sb.AppendLine("<VirtualHost *:443>");
            sb.Append(Body(true));
            sb.AppendLine("</VirtualHost>");
        }
        return sb.ToString();
    }

    // ---- systemd ----------------------------------------------------------

    public static string BuildSystemdUnit(SiteNames names, string appDir, string execStart, int port, string user, bool localDatabase, bool serverGc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Unit]");
        sb.AppendLine($"Description={names.Id} website (ASP.NET Core)");
        sb.AppendLine(localDatabase ? "After=network.target mariadb.service mysql.service" : "After=network.target");
        sb.AppendLine();
        sb.AppendLine("[Service]");
        sb.AppendLine($"WorkingDirectory={appDir}");
        sb.AppendLine($"ExecStart={execStart}");
        sb.AppendLine($"EnvironmentFile={names.EnvFile}");
        sb.AppendLine("Environment=ASPNETCORE_ENVIRONMENT=Production");
        sb.AppendLine($"Environment=ASPNETCORE_URLS=http://127.0.0.1:{port}");
        sb.AppendLine(serverGc ? "Environment=DOTNET_gcServer=1" : "Environment=DOTNET_gcServer=0");
        sb.AppendLine("LimitNOFILE=65535");
        sb.AppendLine("Restart=always");
        sb.AppendLine("RestartSec=10");
        sb.AppendLine("KillSignal=SIGINT");
        sb.AppendLine($"SyslogIdentifier={names.Id}");
        sb.AppendLine($"User={user}");
        sb.AppendLine($"Group={user}");
        sb.AppendLine("NoNewPrivileges=true");
        sb.AppendLine("PrivateTmp=true");
        sb.AppendLine();
        sb.AppendLine("[Install]");
        sb.AppendLine("WantedBy=multi-user.target");
        return sb.ToString();
    }
}
