using AutoMapper;
using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BolNews.Web.ViewComponents
{
    public class LatestNewsViewComponent : ViewComponent
    {
        private readonly IArticleService _articleService;
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly LatestNewsOptions _options;

        public LatestNewsViewComponent(
            IArticleService articleService,
            ICategoryService categoryService,
            IMapper mapper,
            IMemoryCache cache,
            IOptions<LatestNewsOptions> options)
        {
            _articleService = articleService;
            _categoryService = categoryService;
            _mapper = mapper;
            _cache = cache;
            _options = options.Value;
        }

        public async Task<IViewComponentResult> InvokeAsync(int? count = null)
        {
            var resolvedCount = count ?? _options.DefaultCount;
            var cacheKey = $"latest_news_filtered_{resolvedCount}";

            var vm = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);

                var categoryIds = await ResolveCategoryIdsAsync();
                var articles = await _articleService.GetLatestArticlesForCategoriesAsync(categoryIds, resolvedCount);

                return _mapper.Map<List<PublicArticleVM>>(articles);
            });

            return View(vm);
        }

        private async Task<List<int>> ResolveCategoryIdsAsync()
        {
            var allCategories = await _categoryService.GetAllAsyncNew();

            var filtered = _options.IncludedCategorySlugs.Any()
                ? allCategories.Where(c => _options.IncludedCategorySlugs
                    .Contains(c.Slug, StringComparer.OrdinalIgnoreCase))
                : allCategories;

            filtered = filtered.Where(c => !_options.ExcludedCategorySlugs
                .Contains(c.Slug, StringComparer.OrdinalIgnoreCase));

            return filtered.Select(c => c.Id).ToList();
        }
    }
}