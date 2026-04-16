using AutoMapper;
using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers
{
    public class CategoryController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;

        public CategoryController(IArticleService articleService, ICategoryService categoryService, IMapper mapper)
        {
            _articleService = articleService;
            _categoryService = categoryService;
            _mapper = mapper;
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

            // ✅ SEO FROM DATABASE
            ViewBag.CategoryDescription = category.Description;

            ViewBag.MetaTitle = string.IsNullOrWhiteSpace(category.MetaTitle)
                ? category.Name
                : category.MetaTitle;

            ViewBag.MetaDescription = string.IsNullOrWhiteSpace(category.MetaDescription)
                ? $"Latest news in {category.Name}"
                : category.MetaDescription;

            ViewBag.CanonicalUrl = page == 1
                ? $"/news/{category.Slug}"
                : $"/news/{category.Slug}?page={page}";

            ViewBag.CategoryName = category.Name;
            ViewBag.CategorySlug = category.Slug;

            ViewBag.HasNextPage = articles.Count == 10;

            return View(vm);
        }
        //public async Task<IActionResult> Details(string categorySlug, int page = 1)
        //{
        //    int pageSize = 10;

        //    var articles = await _articleService.GetByCategorySlugAsync(categorySlug, page);

        //    if (articles == null || !articles.Any())
        //        return NotFound();

        //    var vm = articles.Select(a => new PublicArticleVM
        //    {
        //        Title = a.Title,
        //        Slug = a.Slug,
        //        CategorySlug = a.Category.Slug,
        //        FeaturedImageUrl = a.FeaturedImageUrl
        //    }).ToList();

        //    ViewBag.CategorySlug = categorySlug;
        //    ViewBag.Page = page;
        //    ViewBag.PageSize = pageSize;
        //    ViewBag.HasNextPage = articles.Count == pageSize;
        //    // SEO
        //    ViewBag.MetaTitle = $"{categorySlug} News - Page {page}";
        //    ViewBag.MetaDescription = $"Latest {categorySlug} news - Page {page}";
        //    ViewBag.CanonicalUrl = page == 1
        //        ? $"/news/{categorySlug}"
        //        : $"/news/{categorySlug}?page={page}";


        //    return View(vm);
        //}
    }
}
