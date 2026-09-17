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

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
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
builder.Services.AddScoped<IWordPressArticleReader, WordPressArticleReader>();
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
app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl = "public,max-age=604800";
    }
});

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<SecurityHeadersMiddleware>();

app.MapHealthChecks("/health");
app.MapAppRoutes();
app.MapControllers();

app.Run();
