using System.Text.Json;
using AutoMapper;
using BolNews.Application.Common;
using BolNews.Application.Interfaces;
using BolNews.Application.Services;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
using BolNews.Web.SEO;
using Microsoft.AspNetCore.Mvc;

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

        public CategoryController(IArticleService articleService, ICategoryService categoryService, IMapper mapper, IUrlService urlService, ISeoService seoService, ICacheService cacheService)
        {
            _articleService = articleService;
            _categoryService = categoryService;
            _mapper = mapper;
            _urlService = urlService;
            _seoService = seoService;
            _cacheService = cacheService;
        }
        public async Task<IActionResult> Details(string categorySlug, int page = 1)
        {
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
                ? $"/news/{category.Slug}"
                : $"/news/{category.Slug}?page={page}";
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
        
    }
}
