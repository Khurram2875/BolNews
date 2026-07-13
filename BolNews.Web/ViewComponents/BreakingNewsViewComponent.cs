using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Web.ViewComponents
{
    public class BreakingNewsViewComponent : ViewComponent
    {
        private readonly IBreakingNewsService _breakingNewsService;
        private readonly IMemoryCache _cache;

        public BreakingNewsViewComponent(
            IBreakingNewsService breakingNewsService,
            IMemoryCache cache)
        {
            _breakingNewsService = breakingNewsService;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            const string cacheKey = "breaking_news";

            var model = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);

                return await _breakingNewsService.GetActiveAsync();
            });

            return View(model);
        }
    }
}