using AutoMapper;
using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Web.ViewComponents
{
    public class TrendingNewsViewComponent : ViewComponent
    {
        private readonly IArticleService _articleService;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;

        public TrendingNewsViewComponent(
            IArticleService articleService,
            IMapper mapper,
            IMemoryCache cache)
        {
            _articleService = articleService;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync(string type = "week", int count = 5)
        {
            // ✅ FIX 1: normalize FIRST
            type = string.IsNullOrEmpty(type) ? "today" : type;

            // ✅ FIX 2: set ViewBag OUTSIDE cache
            ViewBag.Type = type;

            var cacheKey = $"trending_{type}";

            var vm = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                var articles = await _articleService.GetTrendingAsync(count, type);
                return _mapper.Map<List<PublicArticleVM>>(articles);
            });

            return View(vm);
        }
    }
}
