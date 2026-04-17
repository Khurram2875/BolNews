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

            // ✅ Correct category URL
            if (article.Category?.Slug != categorySlug)
            {
                return RedirectToRoutePermanent("articleDetails", new
                {
                    categorySlug = article.Category?.Slug,
                    slug = article.Slug
                });
            }

            // 🔥 Increment View Count (session-safe)
            var viewedKey = $"viewed_article_{article.Id}";

            if (!HttpContext.Session.Keys.Contains(viewedKey))
            {
                await _articleService.IncrementViewCountAsync(article.Id);
                HttpContext.Session.SetString(viewedKey, "true");
            }

            // ✅ Map
            var articleVM = _mapper.Map<PublicArticleVM>(article);

            var relatedArticles = await _articleService.GetRelatedArticlesAsync(
                article.CategoryId,
                article.Id,
                5
            );

            var relatedVM = _mapper.Map<List<PublicArticleVM>>(relatedArticles);

            var pageVM = new ArticleDetailsPageVM
            {
                Article = articleVM,
                RelatedArticles = relatedVM
            };

            // ✅ SEO
            ViewBag.MetaTitle = string.IsNullOrWhiteSpace(article.MetaTitle)
                ? article.Title
                : article.MetaTitle;

            ViewBag.MetaDescription = article.MetaDescription ?? article.Summary;

            ViewBag.CanonicalUrl = $"/news/{article.Category?.Slug}/{article.Slug}";

            ViewBag.CategoryName = article.Category?.Name;
            ViewBag.CategorySlug = article.Category?.Slug;

            return View(pageVM);
        }
    }
}
