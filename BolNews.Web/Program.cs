using BolNews.Application.Interfaces;
using BolNews.Application.Services;
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
builder.Services.AddSignalR();
//builder.Services.AddSignalR().AddAzureSignalR(builder.Configuration["Azure:SignalR:ConnectionString"]!);

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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
