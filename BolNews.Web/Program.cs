using BolNews.Application.Interfaces;
using BolNews.Application.Services;
using BolNews.Domain.Entities;
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
    options.Limits.MaxRequestBodySize =
        1024L * 1024L * 1024L; // 1 GB
});
var app = builder.Build();

//
// One-time CLI commands
//
if (args.Length > 0 &&
    args[0].Equals("reset-admin-password", StringComparison.OrdinalIgnoreCase))
{
    var exitCode = await ResetAdminPasswordAsync(
        app.Services,
        args);

    Environment.ExitCode = exitCode;
    return;
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                      | ForwardedHeaders.XForwardedProto
                      | ForwardedHeaders.XForwardedHost
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/error/{0}");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles();


app.UseRouting();
app.UseSession();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/notificationHub");

app.MapHealthChecks("/health");

app.MapAppRoutes();

using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

static async Task<int> ResetAdminPasswordAsync(
    IServiceProvider services,
    string[] args)
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine(
            "Usage: reset-admin-password <email> <password>");

        Console.Error.WriteLine(
            "Example: reset-admin-password admin@example.com \"YourPasswordHere\"");

        return 1;
    }

    var email = args[1].Trim();
    var password = args[2];

    if (string.IsNullOrWhiteSpace(email))
    {
        Console.Error.WriteLine("Admin email cannot be empty.");
        return 1;
    }

    if (string.IsNullOrWhiteSpace(password))
    {
        Console.Error.WriteLine("Password cannot be empty.");
        return 1;
    }

    using var scope = services.CreateScope();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var user = await userManager.FindByEmailAsync(email);

    if (user == null)
    {
        Console.Error.WriteLine(
            $"User not found: {email}");

        return 2;
    }

    var token = await userManager.GeneratePasswordResetTokenAsync(user);

    var result = await userManager.ResetPasswordAsync(
        user,
        token,
        password);

    if (result.Succeeded)
    {
        Console.WriteLine(
            $"Admin password changed successfully for {email}.");

        return 0;
    }

    Console.Error.WriteLine("Password reset failed:");

    foreach (var error in result.Errors)
    {
        Console.Error.WriteLine(
            $"- {error.Code}: {error.Description}");
    }

    return 3;
}
