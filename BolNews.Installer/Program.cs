using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

try
{
    Console.WriteLine("BolNews CMS first-run database installer\n");
    var server = Ask("MySQL/MariaDB server", "localhost");
    var portText = Ask("Port", "3306");
    if (!ushort.TryParse(portText, out var port) || port == 0) throw new InvalidOperationException("Port must be a number from 1 to 65535.");
    var database = Ask("New database name", "bolnews_cms");
    if (!Regex.IsMatch(database, "^[A-Za-z0-9_]+$")) throw new InvalidOperationException("Use only letters, numbers, and underscores in the database name.");

    var adminUser = Ask("Database administrator username");
    var adminPassword = AskSecret("Database administrator password");
    var appUser = Ask("New application database username", "bolnews_app");
    if (!Regex.IsMatch(appUser, "^[A-Za-z0-9_]+$")) throw new InvalidOperationException("Use only letters, numbers, and underscores in the application username.");
    var appPassword = AskSecret("New application database password");
    if (appPassword.Length < 16) throw new InvalidOperationException("Use an application database password of at least 16 characters.");

    var adminConnection = new MySqlConnectionStringBuilder
    {
        Server = server, Port = port, UserID = adminUser, Password = adminPassword,
        SslMode = MySqlSslMode.Preferred, ConnectionTimeout = 15
    }.ConnectionString;

    await using (var connection = new MySqlConnection(adminConnection))
    {
        await connection.OpenAsync();
        await Execute(connection, $"CREATE DATABASE IF NOT EXISTS `{database}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;");
        await Execute(connection, $"CREATE USER IF NOT EXISTS '{EscapeSql(appUser)}'@'%' IDENTIFIED BY '{EscapeSql(appPassword)}';");
        await Execute(connection, $"ALTER USER '{EscapeSql(appUser)}'@'%' IDENTIFIED BY '{EscapeSql(appPassword)}';");
        await Execute(connection, $"GRANT SELECT, INSERT, UPDATE, DELETE, CREATE TEMPORARY TABLES, LOCK TABLES ON `{database}`.* TO '{EscapeSql(appUser)}'@'%';");
    }

    var appConnection = new MySqlConnectionStringBuilder
    {
        Server = server, Port = port, Database = database, UserID = appUser,
        Password = appPassword, SslMode = MySqlSslMode.Preferred,
        AllowUserVariables = true, ConnectionTimeout = 15, DefaultCommandTimeout = 60
    }.ConnectionString;

    var migrationConnection = new MySqlConnectionStringBuilder(adminConnection) { Database = database }.ConnectionString;
    var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseMySql(migrationConnection, ServerVersion.AutoDetect(migrationConnection), mysql => mysql.EnableRetryOnFailure())
        .Options;
    await using (var db = new AppDbContext(dbOptions))
    {
        Console.WriteLine("Applying CMS database migrations...");
        await db.Database.MigrateAsync();
    }

    var adminEmail = Ask("Initial CMS administrator email");
    var adminPasswordForCms = AskSecret("Initial CMS administrator password");
    if (adminPasswordForCms.Length < 12) throw new InvalidOperationException("The CMS administrator password must be at least 12 characters.");
    await CreateInitialAdmin(appConnection, adminEmail, adminPasswordForCms);

    var configPath = Path.GetFullPath(Ask("Path for appsettings.Production.json", Path.Combine(Environment.CurrentDirectory, "appsettings.Production.json")));
    Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
    JsonObject config;
    if (File.Exists(configPath)) config = JsonNode.Parse(await File.ReadAllTextAsync(configPath)) as JsonObject ?? new JsonObject();
    else config = new JsonObject();
    var connections = config["ConnectionStrings"] as JsonObject;
    if (connections is null)
    {
        connections = new JsonObject();
        config["ConnectionStrings"] = connections;
    }
    connections["DefaultConnection"] = appConnection;
    await File.WriteAllTextAsync(configPath, config.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
    if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(configPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);

    Console.WriteLine("\nInstallation completed.");
    Console.WriteLine($"Database: {database}");
    Console.WriteLine($"Application config: {configPath}");
    Console.WriteLine($"CMS administrator: {adminEmail}");
    Console.WriteLine("The connection string is saved under ConnectionStrings:DefaultConnection in that file. Protect the file; it contains the database password.");
    Console.WriteLine("Start/restart the CMS application to use the new configuration.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Installation failed: {ex.Message}");
    Environment.ExitCode = 1;
}

static string Ask(string label, string? defaultValue = null)
{
    Console.Write(defaultValue is null ? $"{label}: " : $"{label} [{defaultValue}]: ");
    var value = Console.ReadLine()?.Trim();
    return string.IsNullOrEmpty(value) ? defaultValue ?? throw new InvalidOperationException($"{label} is required.") : value;
}

static string AskSecret(string label)
{
    Console.Write($"{label}: ");
    var chars = new List<char>();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) break;
        if (key.Key == ConsoleKey.Backspace) { if (chars.Count > 0) chars.RemoveAt(chars.Count - 1); continue; }
        if (!char.IsControl(key.KeyChar)) chars.Add(key.KeyChar);
    }
    Console.WriteLine();
    return new string(chars.ToArray());
}

static string EscapeSql(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "''", StringComparison.Ordinal);

static async Task Execute(MySqlConnection connection, string sql)
{
    await using var command = new MySqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync();
}

static async Task CreateInitialAdmin(string connectionString, string email, string password)
{
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddDbContext<AppDbContext>(options => options.UseMySql(
        connectionString, ServerVersion.AutoDetect(connectionString)));
    services.AddIdentity<ApplicationUser, IdentityRole>(identity =>
    {
        identity.Password.RequiredLength = 12;
        identity.Password.RequireNonAlphanumeric = true;
        identity.Password.RequireUppercase = true;
        identity.Password.RequireLowercase = true;
        identity.Password.RequireDigit = true;
        identity.User.RequireUniqueEmail = true;
    }).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
    await using var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    if (!await roles.RoleExistsAsync(Roles.Admin))
    {
        var roleResult = await roles.CreateAsync(new IdentityRole(Roles.Admin));
        if (!roleResult.Succeeded) throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
    }
    var existing = await users.FindByEmailAsync(email);
    if (existing is not null)
    {
        if (!await users.IsInRoleAsync(existing, Roles.Admin))
        {
            var existingRoleResult = await users.AddToRoleAsync(existing, Roles.Admin);
            if (!existingRoleResult.Succeeded) throw new InvalidOperationException(string.Join("; ", existingRoleResult.Errors.Select(e => e.Description)));
        }
        return;
    }
    var admin = new ApplicationUser { UserName = email, Email = email, FullName = "System Administrator", EmailConfirmed = true };
    var userResult = await users.CreateAsync(admin, password);
    if (!userResult.Succeeded) throw new InvalidOperationException(string.Join("; ", userResult.Errors.Select(e => e.Description)));
    var addRoleResult = await users.AddToRoleAsync(admin, Roles.Admin);
    if (!addRoleResult.Succeeded) throw new InvalidOperationException(string.Join("; ", addRoleResult.Errors.Select(e => e.Description)));
}
