using AutoMapper;
using BolNews.Application.Common;
using BolNews.Application.Interfaces;
using BolNews.Application.Services;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Configuration;
using BolNews.Web.Interfaces;
using BolNews.Web.SEO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;


namespace BolNews.Web.Controllers
{
    public class CategoryController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;
        private readonly IUrlService _urlService;
        private readonly ISeoService _seoService;
        private readonly ICacheService _cacheService;
        private readonly LatestNewsOptions _latestNewsOptions;

        public CategoryController(
        IArticleService articleService,
        ICategoryService categoryService,
        IMapper mapper,
        IUrlService urlService,
        ISeoService seoService,
        ICacheService cacheService,
        IOptions<LatestNewsOptions> latestNewsOptions)
        {
            _articleService = articleService;
            _categoryService = categoryService;
            _mapper = mapper;
            _urlService = urlService;
            _seoService = seoService;
            _cacheService = cacheService;
            _latestNewsOptions = latestNewsOptions.Value;
        }

        public IActionResult LegacyDetails(string categorySlug)
        {
            return RedirectToRoutePermanent("categoryListing", new { categorySlug });
        }

        public async Task<IActionResult> Details(string categorySlug, int page = 1)
        {
            if (string.Equals(categorySlug, "latest-news", StringComparison.OrdinalIgnoreCase))
                return await LatestNewsVirtualCategory(page);

            var category = await _categoryService.GetBySlugAsync(categorySlug);

            if (category == null)
                return NotFound();

            var articles = await _cacheService.GetOrCreateAsync(
                    CacheKeys.Category(categorySlug, page),
                    async () => await _articleService.GetByCategorySlugAsync(categorySlug, page),
                    5
                );

            if (articles == null || !articles.Any())
                return NotFound();

            var vm = _mapper.Map<List<PublicArticleVM>>(articles);

            var baseUrl = _urlService.GetBaseUrl();
            // ✅ SEO FROM DATABASE
            ViewBag.CategoryDescription = category.Description;

            ViewBag.OgImage = vm.FirstOrDefault()?.FeaturedImageXl;

            ViewBag.MetaTitle = string.IsNullOrWhiteSpace(category.MetaTitle)
                ? category.Name
                : category.MetaTitle;

            ViewBag.MetaDescription = string.IsNullOrWhiteSpace(category.MetaDescription)
                ? $"Latest news in {category.Name}"
                : category.MetaDescription;

            ViewBag.CanonicalUrl = page == 1
                ? $"/{category.Slug}"
                : $"/{category.Slug}?page={page}";
            ViewBag.OgType = "website";

            ViewBag.CategoryName = category.Name;
            ViewBag.CategorySlug = category.Slug;
            

            const int pageSize = 10;

            ViewBag.PageSize = pageSize;
            //ViewBag.HasNextPage = articles.Count == pageSize;
            ViewBag.Page = page; // 🔥 you missed this earlier


            var vm2 = new CategorySectionVM
            {
                Articles = vm,
                CategoryName = category.Name,
                CategorySlug = category.Slug,
                MetaTitle = string.IsNullOrWhiteSpace(category.MetaTitle)
                            ? category.Name
                            : category.MetaTitle,
                MetaDescription = string.IsNullOrWhiteSpace(category.MetaDescription)
                    ? $"Latest news in {category.Name}"
                    : category.MetaDescription,
                BaseUrl = baseUrl,
                Page = page,                       // ✅
                HasNextPage = articles.Count == pageSize
            };
            ViewBag.HasNextPage = vm2.HasNextPage;
            vm2.CategorySchemaJson = await _cacheService.GetOrCreateAsync(
                    $"category_schema_{categorySlug}_{page}",
                    async () => _seoService.BuildCategorySchema(
                        vm2.CategoryName,
                        vm2.CategorySlug,
                        vm2.MetaDescription,
                        vm2.Articles,
                        vm2.BaseUrl
                    ),
                    10
                );
            vm2.BreadcrumbSchemaJson = _seoService.BuildCategoryBreadcrumb(
                vm2.CategoryName,
                vm2.CategorySlug,
                vm2.BaseUrl
            );

            return View(vm2);
        }
        private async Task<IActionResult> LatestNewsVirtualCategory(int page)
        {
            const int pageSize = 20;
            const string virtualSlug = "latest-news";
            const string virtualName = "Latest News";

            var allCategories = await _categoryService.GetAllAsyncNew();
            var includeSlugs = _latestNewsOptions.IncludedCategorySlugs;
            var excludeSlugs = _latestNewsOptions.ExcludedCategorySlugs;

            var categoryIds = (includeSlugs.Any()
                    ? allCategories.Where(c => includeSlugs.Contains(c.Slug, StringComparer.OrdinalIgnoreCase))
                    : allCategories)
                .Where(c => !excludeSlugs.Contains(c.Slug, StringComparer.OrdinalIgnoreCase))
                .Select(c => c.Id)
                .ToList();

            // Fetch a capped merged pool, then paginate in-memory
            var pooled = await _cacheService.GetOrCreateAsync(
                $"latest_news_pool_{pageSize * 5}",
                async () => await _articleService.GetLatestArticlesForCategoriesAsync(categoryIds, pageSize * 5),
                2
            );

            var pageArticles = pooled.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            if (!pageArticles.Any())
                return NotFound();

            var vm = _mapper.Map<List<PublicArticleVM>>(pageArticles);
            var baseUrl = _urlService.GetBaseUrl();

            ViewBag.CategoryDescription = "The latest stories across all our sections.";
            ViewBag.OgImage = vm.FirstOrDefault()?.FeaturedImageXl;
            ViewBag.MetaTitle = virtualName;
            ViewBag.MetaDescription = "Stay up to date with the latest news.";
            ViewBag.CanonicalUrl = page == 1 ? $"/{virtualSlug}" : $"/{virtualSlug}?page={page}";
            ViewBag.OgType = "website";
            ViewBag.CategoryName = virtualName;
            ViewBag.CategorySlug = virtualSlug;
            ViewBag.PageSize = pageSize;
            ViewBag.Page = page;

            var vm2 = new CategorySectionVM
            {
                Articles = vm,
                CategoryName = virtualName,
                CategorySlug = virtualSlug,
                MetaTitle = virtualName,
                MetaDescription = "Stay up to date with the latest news.",
                BaseUrl = baseUrl,
                Page = page,
                HasNextPage = pageArticles.Count == pageSize
            };
            ViewBag.HasNextPage = vm2.HasNextPage;

            vm2.CategorySchemaJson = _seoService.BuildCategorySchema(
                vm2.CategoryName, vm2.CategorySlug, vm2.MetaDescription, vm2.Articles, vm2.BaseUrl);
            vm2.BreadcrumbSchemaJson = _seoService.BuildCategoryBreadcrumb(
                vm2.CategoryName, vm2.CategorySlug, vm2.BaseUrl);

            return View("Details", vm2);
        }

    }
}
