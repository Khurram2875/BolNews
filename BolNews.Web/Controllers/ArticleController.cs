using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace BolNews.Web.Controllers
{
    public class ArticleController : Controller
    {
        private readonly IArticlePageService _articlePageService;
        private readonly IMemoryCache _cache;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _articlePageLocks = new();
        public ArticleController(IArticlePageService articlePageService, IMemoryCache cache)
        {
            _articlePageService = articlePageService;
            _cache = cache;
        }

        public IActionResult LegacyDetails(string categorySlug, string slug)
        {
            return RedirectToRoutePermanent("articleDetails", new { categorySlug, slug });
        }

        public async Task<IActionResult> Details(string categorySlug, string slug)
        {
            var normalizedSlug = slug.ToLowerInvariant();
            var cacheKey = $"article_page_{normalizedSlug}";

            if (!_cache.TryGetValue(cacheKey, out ArticleDetailsPageVM pageVM))
            {
                var keyLock = _articlePageLocks.GetOrAdd(normalizedSlug, _ => new SemaphoreSlim(1, 1));
                await keyLock.WaitAsync();
                try
                {
                    if (!_cache.TryGetValue(cacheKey, out pageVM))
                    {
                        pageVM = await _articlePageService.BuildDetailsPageAsync(slug);
                        if (pageVM != null)
                        {
                            _cache.Set(cacheKey, pageVM, TimeSpan.FromMinutes(2));
                        }
                    }
                }
                finally
                {
                    keyLock.Release();
                }
            }

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
