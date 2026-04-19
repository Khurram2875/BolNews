using System.Text.Json;
using AutoMapper;
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

        public CategoryController(IArticleService articleService, ICategoryService categoryService, IMapper mapper, IUrlService urlService, ISeoService seoService)
        {
            _articleService = articleService;
            _categoryService = categoryService;
            _mapper = mapper;
            _urlService = urlService;
            _seoService = seoService;
        }
        public async Task<IActionResult> Details(string categorySlug, int page = 1)
        {
            var category = await _categoryService.GetBySlugAsync(categorySlug);

            if (category == null)
                return NotFound();

            var articles = await _articleService.GetByCategorySlugAsync(categorySlug, page);

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
                ? $"{baseUrl}/news/{category.Slug}"
                : $"{baseUrl}/news/{category.Slug}?page={page}";

            ViewBag.CategoryName = category.Name;
            ViewBag.CategorySlug = category.Slug;
            ViewBag.page = page;
            ViewBag.HasNextPage = articles.Count == 10;

            
            var vm2 = new CategorySectionVM
            {
                Articles = vm,
                CategoryName = category.Name,
                CategorySlug = category.Slug,
                MetaTitle = string.IsNullOrWhiteSpace(category.MetaTitle)
                            ? category.Name
                            : category.MetaTitle,
                MetaDescription = category.MetaDescription,
                BaseUrl = baseUrl,
                Page = page,                       // ✅
                HasNextPage = articles.Count == 10
            };
            vm2.CategorySchemaJson = _seoService.BuildCategorySchema(
                 vm2.CategoryName,
                 vm2.CategorySlug,
                 vm2.MetaDescription,
                 vm2.Articles,
                 vm2.BaseUrl
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
