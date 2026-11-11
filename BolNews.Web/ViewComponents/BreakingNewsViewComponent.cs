using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using BolNews.Persistence.Context;
using BolNews.Web.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Web.ViewComponents
{
    public class BreakingNewsViewComponent : ViewComponent
    {
        private readonly IBreakingNewsService _breakingNewsService;
        private readonly IMemoryCache _cache;
        private readonly AppDbContext _db;

        public BreakingNewsViewComponent(
            IBreakingNewsService breakingNewsService,
            IMemoryCache cache,
            AppDbContext db)
        {
            _breakingNewsService = breakingNewsService;
            _cache = cache;
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            const string cacheKey = "breaking_news";

            var model = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);

                return await _breakingNewsService.GetActiveAsync();
            });

            // Admin-chosen scroll speed (level 1-10). Falls back to the default if the setting is missing
            // or the SiteSettings table does not exist yet, so the ticker never breaks the page.
            var level = await _cache.GetOrCreateAsync(TickerSettings.CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                try
                {
                    var value = await _db.SiteSettings.AsNoTracking()
                        .Where(x => x.Key == TickerSettings.SpeedKey).Select(x => x.Value).FirstOrDefaultAsync();
                    return TickerSettings.Parse(value);
                }
                catch
                {
                    return TickerSettings.DefaultLevel;
                }
            });
            ViewData["TickerSpeedLevel"] = level;

            return View(model);
        }
    }
}