using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers
{
    public class CategoryController : Controller
    {
        private readonly IArticleService _articleService;

        public CategoryController(IArticleService articleService)
        {
            _articleService = articleService;
        }

        public async Task<IActionResult> Details(string categorySlug, int page = 1)
        {
            int pageSize = 10;

            var articles = await _articleService.GetByCategorySlugAsync(categorySlug, page);

            if (articles == null || !articles.Any())
                return NotFound();

            var vm = articles.Select(a => new PublicArticleVM
            {
                Title = a.Title,
                Slug = a.Slug,
                CategorySlug = a.Category.Slug,
                FeaturedImageUrl = a.FeaturedImageUrl
            }).ToList();

            ViewBag.CategorySlug = categorySlug;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.HasNextPage = articles.Count == pageSize;
            // SEO
            ViewBag.MetaTitle = $"{categorySlug} News - Page {page}";
            ViewBag.MetaDescription = $"Latest {categorySlug} news - Page {page}";
            ViewBag.CanonicalUrl = page == 1
                ? $"/news/{categorySlug}"
                : $"/news/{categorySlug}?page={page}";


            return View(vm);
        }
    }
}
