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
            if (page < 1)
                return RedirectToRoutePermanent("categoryListing", new { categorySlug });

            if (string.Equals(categorySlug, "latest-news", StringComparison.OrdinalIgnoreCase))
                return await LatestNewsVirtualCategory(page);

            var category = await _categoryService.GetBySlugAsync(categorySlug);

            if (category == null)
                return NotFound();

            var parentCategories = await _categoryService.GetParentCategoriesWithChildrenAsync();
            var navigationParentId = category.ParentCategoryId ?? category.Id;
            var navigationParent = parentCategories.FirstOrDefault(x => x.Id == navigationParentId);
            var relatedCategories = navigationParent?.SubCategories?
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .Select(x => new CategoryNavigationItemVM
                {
                    Name = x.Name,
                    Slug = x.Slug,
                    IsSelected = x.Id == category.Id
                })
                .ToList() ?? new List<CategoryNavigationItemVM>();

            const int pageSize = 15;
            const int featureOffset = 1;

            var featuredArticle = await _cacheService.GetOrCreateAsync(
                    $"{CacheKeys.Category(categorySlug, 0)}_featured",
                    async () => (await _articleService.GetCategoryArticlesAsync(categorySlug, 0, 1)).FirstOrDefault(),
                    5
                );

            if (featuredArticle == null)
                return NotFound();

            var articles = await _cacheService.GetOrCreateAsync(
                    CacheKeys.Category(categorySlug, page),
                    async () => await _articleService.GetCategoryArticlesAsync(
                        categorySlug,
                        featureOffset + ((page - 1) * pageSize),
                        pageSize + 1),
                    5
                );

            if (page > 1 && (articles == null || !articles.Any()))
                return NotFound();

            var vm = _mapper.Map<List<PublicArticleVM>>(articles.Take(pageSize));
            var featuredVm = _mapper.Map<PublicArticleVM>(featuredArticle);

            var baseUrl = _urlService.GetBaseUrl();
            // ✅ SEO FROM DATABASE
            ViewBag.CategoryDescription = category.Description;

            ViewBag.OgImage = featuredVm.FeaturedImageXl;

            ViewBag.MetaTitle = string.IsNullOrWhiteSpace(category.MetaTitle)
                ? category.Name +" - "+ category.MetaTitle
                : category.MetaTitle;

            ViewBag.MetaDescription = string.IsNullOrWhiteSpace(category.MetaDescription)
                ? $"Latest news in {category.Name}"
                : category.MetaDescription;

            ViewBag.CanonicalUrl = page == 1
                ? $"/category/{category.Slug}"
                : $"/category/{category.Slug}?page={page}";
            ViewBag.OgType = "website";

            ViewBag.CategoryName = category.Name;
            ViewBag.CategorySlug = category.Slug;
            

            ViewBag.PageSize = pageSize;
            //ViewBag.HasNextPage = articles.Count == pageSize;
            ViewBag.Page = page; // 🔥 you missed this earlier


            var vm2 = new CategorySectionVM
            {
                Articles = vm,
                FeaturedArticle = featuredVm,
                RelatedCategories = relatedCategories,
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
                HasNextPage = articles.Count > pageSize
            };
            ViewBag.HasNextPage = vm2.HasNextPage;
            vm2.CategorySchemaJson = await _cacheService.GetOrCreateAsync(
                    $"category_schema_{categorySlug}_{page}",
                    async () => _seoService.BuildCategorySchema(
                        vm2.CategoryName,
                        vm2.CategorySlug,
                        vm2.MetaDescription,
                        new[] { featuredVm }.Concat(vm2.Articles).ToList(),
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
            if (page < 1)
                return RedirectToRoutePermanent("categoryListing", new { categorySlug = "latest-news" });

            const int pageSize = 15;
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
                $"latest_news_pool_{(pageSize * 5) + 1}",
                async () => await _articleService.GetLatestArticlesForCategoriesAsync(categoryIds, (pageSize * 5) + 1),
                2
            );

            var featuredArticle = pooled.FirstOrDefault();
            var pageArticles = pooled
                .Skip(1 + ((page - 1) * pageSize))
                .Take(pageSize + 1)
                .ToList();

            if (featuredArticle == null || (page > 1 && !pageArticles.Any()))
                return NotFound();

            var vm = _mapper.Map<List<PublicArticleVM>>(pageArticles.Take(pageSize));
            var featuredVm = _mapper.Map<PublicArticleVM>(featuredArticle);
            var baseUrl = _urlService.GetBaseUrl();

            ViewBag.CategoryDescription = "The latest stories across all our sections.";
            ViewBag.OgImage = featuredVm.FeaturedImageXl;
            ViewBag.MetaTitle = virtualName;
            ViewBag.MetaDescription = "Stay up to date with the latest news.";
            ViewBag.CanonicalUrl = page == 1 ? $"/category/{virtualSlug}" : $"/category/{virtualSlug}?page={page}";
            ViewBag.OgType = "website";
            ViewBag.CategoryName = virtualName;
            ViewBag.CategorySlug = virtualSlug;
            ViewBag.PageSize = pageSize;
            ViewBag.Page = page;

            var vm2 = new CategorySectionVM
            {
                Articles = vm,
                FeaturedArticle = featuredVm,
                CategoryName = virtualName,
                CategorySlug = virtualSlug,
                MetaTitle = virtualName,
                MetaDescription = "Stay up to date with the latest news.",
                BaseUrl = baseUrl,
                Page = page,
                HasNextPage = pageArticles.Count > pageSize
            };
            ViewBag.HasNextPage = vm2.HasNextPage;

            vm2.CategorySchemaJson = _seoService.BuildCategorySchema(
                vm2.CategoryName, vm2.CategorySlug, vm2.MetaDescription,
                new[] { featuredVm }.Concat(vm2.Articles).ToList(), vm2.BaseUrl);
            vm2.BreadcrumbSchemaJson = _seoService.BuildCategoryBreadcrumb(
                vm2.CategoryName, vm2.CategorySlug, vm2.BaseUrl);

            return View("Details", vm2);
        }

    }
}
