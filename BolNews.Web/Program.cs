using BolNews.Application.Interfaces;
using BolNews.Application.Services;
using BolNews.Domain.Entities;
using BolNews.Infrastructure.Services;
using BolNews.Infrastructure.Services.Video;
using BolNews.Infrastructure.Services.WordPressMigration;
using BolNews.Persistence;
using BolNews.Persistence.Identity;
using BolNews.Web;
using BolNews.Web.BackgroundServices;
using BolNews.Web.Configuration;
using BolNews.Web.Extensions;
using BolNews.Web.Hubs;
using BolNews.Web.Interfaces;
using BolNews.Web.Middleware;
using BolNews.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddControllersWithViews(options =>
    options.Filters.AddService<BolNews.Web.Filters.AdminTwoFactorEnrollmentFilter>());
builder.Services.AddScoped<BolNews.Web.Filters.AdminTwoFactorEnrollmentFilter>();
builder.Services.AddScoped<UiText>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<BolNews.Web.Services.ShowCarouselService>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});


builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddScoped<INotificationRealtimeService, NotificationRealtimeService>();
builder.Services.AddHostedService<SlaMonitoringWorker>();
builder.Services.AddHostedService<ScheduledPublishingService>();
builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddScoped<IGrammarService, GeminiGrammarService>();
builder.Services.Configure<LatestNewsOptions>(
    builder.Configuration.GetSection(LatestNewsOptions.SectionName));
// WordPressMigration:Source = "Rest" reads over the WordPress REST API (no database access needed);
// anything else keeps the original direct-database reader.
if (string.Equals(builder.Configuration["WordPressMigration:Source"], "Rest", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.Configure<WordPressRestOptions>(builder.Configuration.GetSection(WordPressRestOptions.SectionName));
    builder.Services.AddHttpClient(WordPressRestArticleReader.HttpClientName, client =>
    {
        client.Timeout = TimeSpan.FromSeconds(90);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("BolNews-CMS-WordPress-Migration/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    });
    builder.Services.AddScoped<IWordPressArticleReader, WordPressRestArticleReader>();
}
else
{
    builder.Services.AddScoped<IWordPressArticleReader, WordPressArticleReader>();
}
builder.Services.AddScoped<IWordPressAuthorResolver, WordPressAuthorResolver>();
builder.Services.AddScoped<IWordPressCategoryResolver, WordPressCategoryResolver>();

builder.Services.Configure<WordPressMediaOptions>(
    builder.Configuration.GetSection(
        WordPressMediaOptions.SectionName));

builder.Services.AddSingleton<WordPressMediaSource>();

builder.Services.AddSingleton<WordPressMigrationState>();

builder.Services.AddScoped<WordPressMigrationRepairService>();
builder.Services.AddSingleton<IWordPressMigrationQueue,
    WordPressMigrationQueue>();

builder.Services.AddHostedService<
    WordPressMigrationBackgroundService>();
builder.Services.AddSingleton<
    IVideoThumbnailService,
    FfmpegVideoThumbnailService>();
builder.Services.AddHttpClient(
    "WordPressMedia",
    client =>
    {
        client.Timeout = TimeSpan.FromSeconds(60);

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "BolNews-CMS-WordPress-Migration/1.0");
    });
builder.Services.AddScoped<IWordPressArticleImportService, WordPressArticleImportService>();


builder.Services.AddScoped<IFuelPriceService, FuelPriceService>();

builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection("Email"));

builder.Services.AddScoped<IEmailService, SmtpEmailService>();

builder.Services.AddScoped<
    IWordPressReporterResolver,
    WordPressReporterResolver>();

builder.Services.AddSignalR();
//builder.Services.AddSignalR().AddAzureSignalR(builder.Configuration["Azure:SignalR:ConnectionString"]!);

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddHealthChecks();
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024L * 1024L * 1024L;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                      ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
// Keep the small two-factor HTML response uncompressed while diagnosing browser
// content-decoding failures observed on the local authenticator sign-in page.
app.Use(async (context, next) =>
{
    if (string.Equals(context.Request.Path.Value, "/Account/LoginWithAuthenticator", StringComparison.OrdinalIgnoreCase))
        context.Request.Headers.Remove("Accept-Encoding");

    await next();
});
BolNews.Web.Services.PublisherThemeCatalog.ConfigureWebRoot(app.Environment.WebRootPath);
app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var path = context.Context.Request.Path.Value ?? string.Empty;
        var isVersionedAsset =
            path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase);

        context.Context.Response.Headers.CacheControl = isVersionedAsset
            ? "public,max-age=31536000,immutable"
            : "public,max-age=604800";
    }
});

app.UseRouting();
// Public pages follow the site language (en / ur): sets the request culture so dates, relative times and
// UiText labels render in Urdu. The admin area always stays English / left-to-right.
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (!path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase))
    {
        var profile = await context.RequestServices.GetRequiredService<SiteProfileService>().GetAsync(context.RequestAborted);
        if (PublisherThemeCatalog.IsRtl(profile.LanguageCode))
        {
            var urdu = new System.Globalization.CultureInfo("ur-PK");
            System.Globalization.CultureInfo.CurrentCulture = urdu;
            System.Globalization.CultureInfo.CurrentUICulture = urdu;
        }
    }
    await next();
});
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<SecurityHeadersMiddleware>();

app.MapHealthChecks("/health");
app.MapAppRoutes();
app.MapControllers();

app.Run();
