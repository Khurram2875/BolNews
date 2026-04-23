using AutoMapper;
using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
using BolNews.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers
{
    public class ArticleController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly IMapper _mapper;
        private readonly IUrlService _urlService;
        private readonly ISeoService _seoService;
        private readonly IInternalLinkingService _internalLinkingService;
        private readonly ICacheService _cacheService;
        public ArticleController(IArticleService articleService, IMapper mapper, IUrlService urlService, ISeoService seoService, IInternalLinkingService internalLinkingService, ICacheService cacheService)
        {
            _articleService = articleService;
            _mapper = mapper;
            _urlService = urlService;
            _seoService = seoService;
            _internalLinkingService = internalLinkingService;
            _cacheService = cacheService;
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
            articleVM.Content = await _cacheService.GetOrCreateAsync(
                    $"article_content_{article.Id}",
                    async () => await _internalLinkingService.InjectInternalLinksAsync(articleVM.Content),
                    10
                );

            var relatedArticles = await _cacheService.GetOrCreateAsync(
                $"related_{article.Id}",
                async () => await _articleService.GetRelatedArticlesAsync(
                    article.CategoryId,
                    article.Id,
                    5
                ),
                10
            );

            var relatedVM = _mapper.Map<List<PublicArticleVM>>(relatedArticles);
            var baseUrl = _urlService.GetBaseUrl();
            var pageVM = new ArticleDetailsPageVM
            {
                Article = articleVM,
                RelatedArticles = relatedVM,
                BaseUrl = baseUrl
            };

            // ✅ SEO
            ViewBag.OgImage = articleVM.FeaturedImageXl;

            ViewBag.MetaTitle = string.IsNullOrWhiteSpace(article.MetaTitle)
                ? article.Title
                : article.MetaTitle;

            ViewBag.MetaDescription = article.MetaDescription ?? article.Summary;

            ViewBag.CanonicalUrl = $"/news/{article.Category?.Slug}/{article.Slug}";

            ViewBag.CategoryName = article.Category?.Name;
            ViewBag.CategorySlug = article.Category?.Slug;

            pageVM.ArticleSchemaJson = _seoService.BuildArticleSchema(pageVM.Article, pageVM.BaseUrl);
            pageVM.BreadcrumbSchemaJson = _seoService.BuildBreadcrumb(pageVM.Article, pageVM.BaseUrl);

            return View(pageVM);
        }
    }
}
