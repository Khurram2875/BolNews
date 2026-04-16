using AutoMapper;
using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Web.ViewComponents
{
    public class BreakingNewsViewComponent : ViewComponent
    {
        private readonly IArticleService _articleService;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;

        public BreakingNewsViewComponent(
            IArticleService articleService,
            IMapper mapper,
            IMemoryCache cache)
        {
            _articleService = articleService;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync(int count = 5)
        {
            var cacheKey = "breaking_news";

            var vm = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);

                var articles = await _articleService.GetBreakingNewsAsync(count);
                return _mapper.Map<List<PublicArticleVM>>(articles);
            });

            return View(vm);
        }
    }
}
