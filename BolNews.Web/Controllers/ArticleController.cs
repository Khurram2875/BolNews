using BolNews.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers
{
    public class ArticleController : Controller
    {
        private readonly IArticlePageService _articlePageService;

        public ArticleController(IArticlePageService articlePageService)
        {
            _articlePageService = articlePageService;
        }

        public IActionResult LegacyDetails(string categorySlug, string slug)
        {
            return RedirectToRoutePermanent("articleDetails", new { categorySlug, slug });
        }

        public async Task<IActionResult> Details(string categorySlug, string slug)
        {
            var pageVM = await _articlePageService.BuildDetailsPageAsync(slug);
            if (pageVM == null)
                return NotFound();

            if (!string.Equals(pageVM.Article.CategorySlug, categorySlug, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToRoutePermanent("articleDetails", new
                {
                    categorySlug = pageVM.Article.CategorySlug,
                    slug = pageVM.Article.Slug
                });
            }

            await _articlePageService.TrackArticleEngagementAsync(pageVM.Article.Id, HttpContext.Session);

            ViewBag.OgImage = pageVM.Article.FeaturedImageXl;
            ViewBag.MetaTitle = string.IsNullOrWhiteSpace(pageVM.Article.MetaTitle)
                ? pageVM.Article.Title
                : pageVM.Article.MetaTitle;
            ViewBag.MetaDescription = pageVM.Article.MetaDescription ?? pageVM.Article.Summary;
            ViewBag.CanonicalUrl = $"/{pageVM.Article.CategorySlug}/{pageVM.Article.Slug}";
            ViewBag.CategoryName = pageVM.Article.CategoryName;
            ViewBag.CategorySlug = pageVM.Article.CategorySlug;
            ViewBag.OgType = "article";
            ViewBag.MetaKeywords = pageVM.MetaKeywords;
            ViewBag.ArticleTags = pageVM.Article.ArticleTags;
            ViewBag.Author = pageVM.Article.AuthorName;
            ViewBag.Source = pageVM.Article.ReporterName;

            return View(pageVM);
        }
    }
}
