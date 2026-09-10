using AutoMapper;
using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
using System.Diagnostics;

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
        private readonly IArticleEngagementQueue _engagementQueue;
        private readonly ILogger<ArticlePageService> _logger;

        public ArticlePageService(
            IArticleService articleService,
            IMapper mapper,
            IUrlService urlService,
            ISeoService seoService,
            IInternalLinkingService internalLinkingService,
            ICacheService cacheService,
            IAnalyticsService analyticsService,
            IArticleEngagementQueue engagementQueue,
            ILogger<ArticlePageService> logger)
        {
            _articleService = articleService;
            _mapper = mapper;
            _urlService = urlService;
            _seoService = seoService;
            _internalLinkingService = internalLinkingService;
            _cacheService = cacheService;
            _analyticsService = analyticsService;
            _engagementQueue = engagementQueue;
            _logger = logger;
        }
        //temporary logging added for performance monitoring
        public async Task<ArticleDetailsPageVM?> BuildDetailsPageAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            // ---------------------------------------------------------
            // 1. Get article
            // ---------------------------------------------------------

            var article = await _articleService.GetPublicArticleBySlugAsync(slug);

            if (article == null)
                return null;
            // ---------------------------------------------------------
            // 2. Map article
            // ---------------------------------------------------------
           
            var articleVM = new PublicArticleVM
            {
                Id = article.Id,
                Title = article.Title,
                Slug = article.Slug,

                MetaTitle = article.MetaTitle,
                MetaDescription = article.MetaDescription,

                Summary = article.Summary,
                Content = article.Content,

                FeaturedImageThumb = article.FeaturedImageThumb,
                FeaturedImageMedium = article.FeaturedImageMedium,
                FeaturedImageLarge = article.FeaturedImageLarge,
                FeaturedImageXl = article.FeaturedImageXl,

                FeaturedImageAltText = article.FeaturedImageAltText,
                FeaturedImageCaption = article.FeaturedImageCaption,
                FeaturedImageCredit = article.FeaturedImageCredit,

                CategoryName = article.CategoryName,
                CategorySlug = article.CategorySlug,

                AuthorName = article.AuthorName,
                AuthorSlug = article.AuthorSlug,
                AuthorImage = article.AuthorImage,

                ReporterName = article.ReporterName,
                ReporterSourceName = article.ReporterSourceName,

                ArticleTags = article.ArticleTags,
                FeaturedImageTags = article.FeaturedImageTags,

                PublishedAt = article.PublishedAt,
                UpdatedAt = article.UpdatedAt
            };
            
            // ---------------------------------------------------------
            // 3. Internal links
            // ---------------------------------------------------------
            articleVM.Content = await _cacheService.GetOrCreateAsync(
                $"article_content_{article.Id}",
                () => _internalLinkingService.InjectInternalLinksAsync(
                    articleVM.Content),
                10);

            // ---------------------------------------------------------
            // 4. Related articles
            // ---------------------------------------------------------
            var tagIds = article.ArticleTagIds;
            var relatedArticles =
                await _articleService.GetRelatedArticlesAsync(
                    article.CategoryId,
                    article.Id,
                    tagIds,
                    12);

            // ---------------------------------------------------------
            // 5. Build VM + SEO
            // ---------------------------------------------------------
            var pageVM = new ArticleDetailsPageVM
            {
                Article = articleVM,

                RelatedArticles =
                    _mapper.Map<List<PublicArticleVM>>(relatedArticles),

                BaseUrl = _urlService.GetBaseUrl()
            };

            pageVM.MetaKeywords =
                _seoService.BuildKeywords(pageVM.Article);

            pageVM.ArticleSchemaJson =
                _seoService.BuildArticleSchema(
                    pageVM.Article,
                    pageVM.BaseUrl);

            pageVM.BreadcrumbSchemaJson =
                _seoService.BuildBreadcrumb(
                    pageVM.Article,
                    pageVM.BaseUrl);

            return pageVM;
        }

        //original code commented out temporarily for testing purposes
        //public async Task<ArticleDetailsPageVM?> BuildDetailsPageAsync(string slug)
        //{
        //    if (string.IsNullOrWhiteSpace(slug)) return null;

        //    var article = await _articleService.GetBySlugAsync(slug);
        //    if (article == null || article.IsDeleted) return null;

        //    var articleVM = _mapper.Map<PublicArticleVM>(article);
        //    articleVM.AuthorImage = article.Author?.ProfileImageUrl ?? string.Empty;
        //    articleVM.AuthorSlug = article.Author?.Slug ?? string.Empty;

        //    articleVM.Content = await _cacheService.GetOrCreateAsync(
        //        $"article_content_{article.Id}",
        //        async () => await _internalLinkingService.InjectInternalLinksAsync(articleVM.Content),
        //        10);

        //    var relatedArticles = await _cacheService.GetOrCreateAsync(
        //        $"related_{article.Id}",
        //        async () => await _articleService.GetRelatedArticlesAsync(article.CategoryId, article.Id, 5),
        //        10);

        //    var pageVM = new ArticleDetailsPageVM
        //    {
        //        Article = articleVM,
        //        RelatedArticles = _mapper.Map<List<PublicArticleVM>>(relatedArticles),
        //        BaseUrl = _urlService.GetBaseUrl()
        //    };

        //    pageVM.MetaKeywords = _seoService.BuildKeywords(pageVM.Article);
        //    pageVM.ArticleSchemaJson = _seoService.BuildArticleSchema(pageVM.Article, pageVM.BaseUrl);
        //    pageVM.BreadcrumbSchemaJson = _seoService.BuildBreadcrumb(pageVM.Article, pageVM.BaseUrl);

        //    return pageVM;
        //}

        public Task TrackArticleEngagementAsync(int articleId, ISession session)
        {
            var viewedKey = $"viewed_article_{articleId}";

            var incrementViewCount = !session.Keys.Contains(viewedKey);

            if (incrementViewCount)
            {
                session.SetString(viewedKey, "true");
            }

            _engagementQueue.TryEnqueue(
                articleId,
                incrementViewCount);

            return Task.CompletedTask;
        }
    }
}
