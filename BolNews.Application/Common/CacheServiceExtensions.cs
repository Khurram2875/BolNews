using BolNews.Application.Interfaces;

namespace BolNews.Application.Common;

public static class CacheServiceExtensions
{
    public static void InvalidateHomePage(this ICacheService cacheService)
    {
        cacheService.Remove(CacheKeys.HomePage);
        cacheService.Remove(CacheKeys.HomePageIndex);
        cacheService.Remove(CacheKeys.HomePageIndex1);
    }
}
