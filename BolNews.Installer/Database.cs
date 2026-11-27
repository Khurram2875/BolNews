using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace CmsInstaller;

/// <summary>Everything that needs EF Core or MySqlConnector: remote administration, connection tests, schema creation and seeding.</summary>
public static class DatabaseSetup
{
    public static string AdminConnection(string host, int port, string user, string password) =>
        new MySqlConnectionStringBuilder
        {
            Server = host, Port = (uint)port, UserID = user, Password = password,
            SslMode = MySqlSslMode.Preferred, ConnectionTimeout = 15
        }.ConnectionString;

    /// <summary>Runs statements on another database server using the administrator login.</summary>
    public static async Task RunSqlAsync(string adminConnection, IEnumerable<string> statements)
    {
        await using var connection = new MySqlConnection(adminConnection);
        await connection.OpenAsync();
        foreach (var sql in statements)
        {
            await using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>Returns null when the connection works, otherwise the reason it does not.</summary>
    public static async Task<string?> TestConnectionAsync(string connectionString)
    {
        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            return null;
        }
        catch (Exception ex) { return ex.Message; }
    }

    /// <summary>Creates the schema (or applies pending migrations), seeds defaults, sets the site language and creates the first administrator.</summary>
    public static async Task InstallSchemaAsync(string migrationConnection, string appConnection, string language, string adminEmail, string adminPassword)
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(migrationConnection, ServerVersion.AutoDetect(migrationConnection), mysql => mysql.EnableRetryOnFailure())
            .Options;
        await using (var db = new AppDbContext(dbOptions))
        {
            await ApplyFreshSchemaOrMigrations(db);
            var profile = await db.SiteProfiles.OrderBy(x => x.Id).FirstAsync();
            if (profile.LanguageCode != language)
            {
                profile.LanguageCode = language;
                if (language == "ur") profile.FontFamily = "Noto Nastaliq Urdu";
                await db.SaveChangesAsync();
            }
        }
        await CreateInitialAdmin(appConnection, adminEmail, adminPassword);
    }

    internal static async Task ApplyFreshSchemaOrMigrations(AppDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            await using var check = connection.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name NOT IN ('__EFMigrationsHistory', '__EFMigrationsLock')";
            var applicationTableCount = Convert.ToInt32(await check.ExecuteScalarAsync());

            if (applicationTableCount == 0)
            {
                Ui.Hint("Empty database detected. Creating the current CMS schema...");
                await using (var cleanup = connection.CreateCommand())
                {
                    cleanup.CommandText = "DROP TABLE IF EXISTS `__EFMigrationsLock`";
                    await cleanup.ExecuteNonQueryAsync();
                    cleanup.CommandText = "DROP TABLE IF EXISTS `__EFMigrationsHistory`";
                    await cleanup.ExecuteNonQueryAsync();
                }

                await db.Database.EnsureCreatedAsync();
                await CreateMigrationHistoryAndMarkCurrent(db, connection);
                Ui.Hint("Fresh schema created and migration baseline recorded.");
            }
            else
            {
                Ui.Hint("Applying CMS database migrations...");
                await db.Database.MigrateAsync();
            }
        }
        finally
        {
            await connection.CloseAsync();
        }

        await SeedProductDefaults(db);
    }

    internal static async Task CreateMigrationHistoryAndMarkCurrent(AppDbContext db, System.Data.Common.DbConnection connection)
    {
        await using (var createHistory = connection.CreateCommand())
        {
            createHistory.CommandText = "CREATE TABLE `__EFMigrationsHistory` (`MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL, `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL, PRIMARY KEY (`MigrationId`)) CHARACTER SET=utf8mb4;";
            await createHistory.ExecuteNonQueryAsync();
        }

        var migrations = db.Database.GetMigrations();
        var productVersion = db.Model.GetProductVersion() ?? "8.0.25";
        foreach (var migrationId in migrations)
        {
            await using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES (@migrationId, @productVersion)";
            var migrationParameter = insert.CreateParameter();
            migrationParameter.ParameterName = "@migrationId";
            migrationParameter.Value = migrationId;
            insert.Parameters.Add(migrationParameter);
            var versionParameter = insert.CreateParameter();
            versionParameter.ParameterName = "@productVersion";
            versionParameter.Value = productVersion;
            insert.Parameters.Add(versionParameter);
            await insert.ExecuteNonQueryAsync();
        }
    }

    internal static async Task SeedProductDefaults(AppDbContext db)
    {
        if (!await db.SiteProfiles.AnyAsync())
        {
            db.SiteProfiles.Add(new SiteProfile { Id = 1, UpdatedAt = DateTime.UtcNow });
        }

        if (!await db.AdPlacements.AnyAsync())
        {
            var timestamp = DateTime.UtcNow;
            db.AdPlacements.AddRange(new[]
            {
                Placement("site-top-leaderboard", "Top leaderboard", "site.top", "Bolnews_Top_Leaderboard_ATF", "top-leaderboard-atf", "bn-ad-slot-leaderboard", timestamp),
                Placement("site-bottom-banner", "Bottom banner", "site.bottom", "Bolnews_Bottom_BTF", "bottom-btf", "bn-ad-slot-banner", timestamp),
                Placement("home-sidebar-top", "Homepage sidebar top", "home.sidebar.top", "Bolnews_Sidebar_Top_ATF", "sidebar-top-atf", "bn-ad-slot-sidebar", timestamp),
                Placement("home-sidebar-bottom", "Homepage sidebar bottom", "home.sidebar.bottom", "Bolnews_Sidebar_Bottom_BTF", "sidebar-bottom-btf", "bn-ad-slot-sidebar", timestamp),
                Placement("home-content-incontent", "Homepage content ad", "home.content", "Bolnews_Incontent_Lazy", "incontent", "bn-ad-slot-incontent", timestamp),
                Placement("article-sidebar-top", "Article sidebar top", "article.sidebar.top", "Bolnews_Sidebar_Top_ATF", "sidebar-top-atf", "bn-ad-slot-sidebar", timestamp),
                Placement("article-sidebar-bottom", "Article sidebar bottom", "article.sidebar.bottom", "Bolnews_Sidebar_Bottom_BTF", "sidebar-bottom-btf", "bn-ad-slot-sidebar", timestamp),
                Placement("article-inline-incontent", "Article body ad", "article.inline", "Bolnews_Incontent_Lazy", "incontent", "bn-ad-slot-incontent", timestamp, 2, "afterParagraph"),
                Placement("category-sidebar-top", "Category sidebar top", "category.sidebar.top", "Bolnews_Sidebar_Top_ATF", "sidebar-top-atf", "bn-ad-slot-sidebar", timestamp),
                Placement("category-sidebar-bottom", "Category sidebar bottom", "category.sidebar.bottom", "Bolnews_Sidebar_Bottom_BTF", "sidebar-bottom-btf", "bn-ad-slot-sidebar", timestamp),
                Placement("category-content-incontent", "Category content ad", "category.content", "Bolnews_Incontent_Lazy", "incontent", "bn-ad-slot-incontent", timestamp),
                Placement("tag-sidebar-top", "Tag sidebar top", "tag.sidebar.top", "Bolnews_Sidebar_Top_ATF", "sidebar-top-atf", "bn-ad-slot-sidebar", timestamp),
                Placement("tag-sidebar-bottom", "Tag sidebar bottom", "tag.sidebar.bottom", "Bolnews_Sidebar_Bottom_BTF", "sidebar-bottom-btf", "bn-ad-slot-sidebar", timestamp),
                Placement("tag-content-incontent", "Tag content ad", "tag.content", "Bolnews_Incontent_Lazy", "incontent", "bn-ad-slot-incontent", timestamp),
                Placement("search-sidebar-top", "Search sidebar top", "search.sidebar.top", "Bolnews_Sidebar_Top_ATF", "sidebar-top-atf", "bn-ad-slot-sidebar", timestamp),
                Placement("search-sidebar-bottom", "Search sidebar bottom", "search.sidebar.bottom", "Bolnews_Sidebar_Bottom_BTF", "sidebar-bottom-btf", "bn-ad-slot-sidebar", timestamp),
                Placement("search-content-incontent", "Search results ad", "search.content", "Bolnews_Incontent_Lazy", "incontent", "bn-ad-slot-incontent", timestamp),
                Placement("video-sidebar-top", "Video sidebar top", "video.sidebar.top", "Bolnews_Sidebar_Top_ATF", "sidebar-top-atf", "bn-ad-slot-sidebar", timestamp)
            });
        }

        await db.SaveChangesAsync();
    }

    internal static AdPlacement Placement(string key, string displayName, string zone, string elementId, string providerUnit, string containerClass, DateTime updatedAt, int? paragraphIndex = null, string insertionMode = "zone") => new()
    {
        Key = key,
        DisplayName = displayName,
        ZoneKey = zone,
        ElementId = elementId,
        ProviderUnit = providerUnit,
        ContainerClass = containerClass,
        IsEnabled = true,
        SortOrder = 0,
        ParagraphIndex = paragraphIndex,
        InsertionMode = insertionMode,
        UpdatedAt = updatedAt
    };

    internal static async Task CreateInitialAdmin(string connectionString, string email, string password)
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
        foreach (var roleName in Roles.SupportedRoleNames)
        {
            if (await roles.RoleExistsAsync(roleName))
                continue;

            var roleResult = await roles.CreateAsync(new IdentityRole(roleName));
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
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
}
