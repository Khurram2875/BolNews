using BolNews.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Web.ViewComponents
{
    public class SmartTrendingViewComponent : ViewComponent
    {
        private readonly IArticleService _articleService;
        private readonly IMemoryCache _cache;

        public SmartTrendingViewComponent(
            IArticleService articleService,
            IMemoryCache cache)
        {
            _articleService = articleService;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync(int count = 5)
        {
            var cacheKey = $"smart-trending:{count}";

            var articles = await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                    return await _articleService.GetTopRankedPublishedAsync(count);
                });

            return View(articles ?? Enumerable.Empty<BolNews.Domain.Entities.Article>());
        }
    }
}
