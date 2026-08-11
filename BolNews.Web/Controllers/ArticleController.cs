using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
using BolNews.Application.Common;
using BolNews.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers
{
    public class ArticleController : Controller
    {
        private readonly IArticlePageService _articlePageService;
        private readonly ICacheService _cacheService;

        public ArticleController(IArticlePageService articlePageService, ICacheService cacheService)
        {
            _articlePageService = articlePageService;
            _cacheService = cacheService;
        }

        public IActionResult LegacyDetails(string categorySlug, string slug)
        {
            return RedirectToRoutePermanent("articleDetails", new { categorySlug, slug });
        }

        public async Task<IActionResult> Details(string categorySlug, string slug)
        {
            var cacheKey = CacheKeys.ArticlePage(slug);
            var pageVM = await _cacheService.GetOrCreateAsync<ArticleDetailsPageVM?>(
                cacheKey,
                async () => await _articlePageService.BuildDetailsPageAsync(slug),
                2);

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
