using BolNews.Application.Interfaces;
using BolNews.Application.Interfaces.Scoring;
using BolNews.Application.Services;
using BolNews.Application.Services.Scoring;
using BolNews.Application.Services.Scoring.Providers;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using BolNews.Persistence.Repositories;
using BolNews.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BolNews.Infrastructure.Services;
using Microsoft.Extensions.Logging;


namespace BolNews.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ////Get the connection string from configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            //Get the connection string from Azure
            //var connectionString = "server=bolnewsdb.mysql.database.azure.com; port=3306; database=bolnewsdb; user=admin_user; password =Bol12345";
            ////local DB connection string
            //var connectionString = "server=192.168.75.129; port=3306; database=BolNewsDB; user=admin_user; password =Bol12345; " +
              //          "Pooling=true; MinimumPoolSize=20; MaximumPoolSize=300; ConnectionTimeout=30; DefaultCommandTimeout=60;";
            services.AddDbContextPool<AppDbContext>(options =>
            {
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString),
                    mysqlOptions =>
                    {
                        mysqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null);
                    });
                //temporary logging to console for debugging purposes
                options.LogTo(
                    Console.WriteLine,
                    LogLevel.Information);
            },
            poolSize: 256);

            // Identity setup
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });
            services.AddScoped<IArticleRepository, ArticleRepository>();
            services.AddScoped<IEditorialPlacementRepository, EditorialPlacementRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            // Add the correct using directive for IMediaAssetRepository and MediaAssetRepository
            
            services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
            services.AddScoped<IAuthorRepository, AuthorRepository>();
            services.AddScoped<IReporterRepository, ReporterRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
            services.AddScoped<IArticleScoringService, ArticleScoringService>();
            services.AddScoped<IArticleRevisionRepository, ArticleRevisionRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            //Score providers
            services.AddScoped<IScoreProvider, SeoScoreProvider>();
            services.AddScoped<IScoreProvider, FreshnessScoreProvider>();
            services.AddScoped<IScoreProvider, EngagementScoreProvider>();
            services.AddScoped<IScoreProvider, PopularityScoreProvider>();
            services.AddScoped<IScoreProvider, EditorialScoreProvider>();
            services.AddScoped<IScoreProvider, CredibilityScoreProvider>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ISlaService, SlaService>();
            services.AddScoped<ISlaEscalationService, SlaEscalationService>();
            services.AddScoped<IOperationalAnalyticsService, OperationalAnalyticsService>();
            services.AddScoped<IArticleLockService, ArticleLockService>();
            services.AddScoped<IArticleDiscussionService, ArticleDiscussionService>();
            services.AddSingleton<IPresenceTracker, PresenceTracker>();
            services.AddScoped<IBreakingNewsRepository, BreakingNewsRepository>();
            services.AddScoped<IBreakingNewsService, BreakingNewsService>();
            services.AddSingleton<ArticleEngagementQueue>();
            services.AddSingleton<IArticleEngagementQueue>(
                sp => sp.GetRequiredService<ArticleEngagementQueue>());

            services.AddHostedService<ArticleEngagementWorker>();

            return services;
        }
    }
}
