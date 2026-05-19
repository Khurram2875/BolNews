using AutoMapper;
using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;

namespace BolNews.Web.Services
{
    public class ArticlePageService : IArticlePageService
    {
        private readonly IArticleService _articleService;
        private readonly IMapper _mapper;
        private readonly IUrlService _urlService;
        private readonly ISeoService _seoService;
        private readonly IInternalLinkingService _internalLinkingService;
        private readonly ICacheService _cacheService;
        private readonly IAnalyticsService _analyticsService;

        public ArticlePageService(
            IArticleService articleService,
            IMapper mapper,
            IUrlService urlService,
            ISeoService seoService,
            IInternalLinkingService internalLinkingService,
            ICacheService cacheService,
            IAnalyticsService analyticsService)
        {
            _articleService = articleService;
            _mapper = mapper;
            _urlService = urlService;
            _seoService = seoService;
            _internalLinkingService = internalLinkingService;
            _cacheService = cacheService;
            _analyticsService = analyticsService;
        }

        public async Task<ArticleDetailsPageVM?> BuildDetailsPageAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;

            var article = await _articleService.GetBySlugAsync(slug);
            if (article == null || article.IsDeleted) return null;

            var articleVM = _mapper.Map<PublicArticleVM>(article);
            articleVM.AuthorImage = article.Author?.ProfileImageUrl ?? string.Empty;
            articleVM.AuthorSlug = article.Author?.Slug ?? string.Empty;

            articleVM.Content = await _cacheService.GetOrCreateAsync(
                $"article_content_{article.Id}",
                async () => await _internalLinkingService.InjectInternalLinksAsync(articleVM.Content),
                10);

            var relatedArticles = await _cacheService.GetOrCreateAsync(
                $"related_{article.Id}",
                async () => await _articleService.GetRelatedArticlesAsync(article.CategoryId, article.Id, 5),
                10);

            var pageVM = new ArticleDetailsPageVM
            {
                Article = articleVM,
                RelatedArticles = _mapper.Map<List<PublicArticleVM>>(relatedArticles),
                BaseUrl = _urlService.GetBaseUrl()
            };

            pageVM.ArticleSchemaJson = _seoService.BuildArticleSchema(pageVM.Article, pageVM.BaseUrl);
            pageVM.BreadcrumbSchemaJson = _seoService.BuildBreadcrumb(pageVM.Article, pageVM.BaseUrl);

            return pageVM;
        }

        public async Task TrackArticleEngagementAsync(int articleId, ISession session)
        {
            await _analyticsService.TrackImpressionAsync(articleId);

            var viewedKey = $"viewed_article_{articleId}";
            if (!session.Keys.Contains(viewedKey))
            {
                await _articleService.IncrementViewCountAsync(articleId);
                session.SetString(viewedKey, "true");
            }
        }
    }
}
