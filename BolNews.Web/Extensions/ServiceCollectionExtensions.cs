using BolNews.Application.Interfaces;
using BolNews.Application.Models;
using BolNews.Application.Services;
using BolNews.Infrastructure.Services;
using BolNews.Web.Interfaces;
using BolNews.Web.Services;

namespace BolNews.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddAutoMapper(typeof(MappingProfile));

        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAuthorService, AuthorService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IUrlService, UrlService>();
        services.AddScoped<ISeoService, SeoService>();
        services.AddScoped<ISitemapService, SitemapService>();
        services.AddScoped<IDiscoverService, DiscoverService>();
        services.AddScoped<IHeadlineService, HeadlineService>();
        services.AddScoped<ITrendingService, TrendingService>();
        services.AddScoped<IArticlePageService, ArticlePageService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IInternalLinkingService, InternalLinkingService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IAdService, AdService>();
        services.AddScoped<IArticleRevisionService, ArticleRevisionService>();

        services.AddMemoryCache();

        services.AddHttpClient<IGoogleTrendsService, GoogleTrendsService>();
        services.AddHttpClient<IWeatherService, WeatherService>(client =>
        {
            client.BaseAddress = new Uri("https://api.weatherapi.com/v1/");
        });
        services.Configure<WeatherApiOptions>(configuration.GetSection("WeatherApi"));

        services.AddHttpClient<IForexService, ForexService>(client =>
        {
            client.BaseAddress = new Uri("https://v6.exchangerate-api.com/v6/");
        });
        services.Configure<ForexApiOptions>(configuration.GetSection("ForexApi"));

        services.AddHttpClient<IGoldRateService, GoldRateService>();
        services.Configure<GoldApiOptions>(configuration.GetSection("RapidApi"));
        services.Configure<AdOptions>(configuration.GetSection("Ads"));

        return services;
    }
}
