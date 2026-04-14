using AutoMapper;
using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers
{
    public class ArticleController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly IMapper _mapper;
        public ArticleController(IArticleService articleService, IMapper mapper)
        {
            _articleService = articleService;
            _mapper = mapper;
        }

        public async Task<IActionResult> Details(string categorySlug, string slug)
        {
            if (string.IsNullOrEmpty(slug))
                return NotFound();

            var article = await _articleService.GetBySlugAsync(slug);

            if (article == null || article.IsDeleted)
                return NotFound();

            // ✅ Enforce correct category URL (SEO)
            if (article.Category?.Slug != categorySlug)
            {
                return RedirectToRoutePermanent("articleDetails", new
                {
                    categorySlug = article.Category?.Slug,
                    slug = article.Slug
                });
            }

            // ✅ Map to Public VM
            var vm = _mapper.Map<PublicArticleVM>(article);

            // ✅ SEO
            ViewBag.MetaTitle = string.IsNullOrWhiteSpace(article.MetaTitle)
                ? article.Title
                : article.MetaTitle;

            ViewBag.MetaDescription = article.MetaDescription ?? article.Summary;

            ViewBag.CanonicalUrl = $"/news/{article.Category?.Slug}/{article.Slug}";

            // ✅ Breadcrumb support
            ViewBag.CategoryName = article.Category?.Name;
            ViewBag.CategorySlug = article.Category?.Slug;

            return View(vm);
        }
    }
}
