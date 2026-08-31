using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services.WordPressMigration
{
    public class WordPressArticleImportService
    : IWordPressArticleImportService
    {
        private const string SourceSystem = "WordPress";

        private readonly IWordPressArticleReader _reader;
        private readonly IWordPressAuthorResolver _authorResolver;
        private readonly IWordPressCategoryResolver _categoryResolver;
        private readonly IWordPressReporterResolver _reporterResolver;
        private readonly WordPressMediaSource _mediaSource;

        private readonly IArticleRepository _articleRepository;
        private readonly IImageService _imageService;
        private readonly ITagService _tagService;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;

        private readonly ILogger<WordPressArticleImportService> _logger;

        public WordPressArticleImportService(
            IWordPressArticleReader reader,
            IWordPressAuthorResolver authorResolver,
            IWordPressCategoryResolver categoryResolver,
            IWordPressReporterResolver reporterResolver,
            IArticleRepository articleRepository,
            IImageService imageService,
            ITagService tagService,
            IHttpClientFactory httpClientFactory,
           IWebHostEnvironment environment,
            ILogger<WordPressArticleImportService> logger,
            WordPressMediaSource mediaSource)
        {
            _reader = reader;
            _authorResolver = authorResolver;
            _categoryResolver = categoryResolver;
            _reporterResolver = reporterResolver;
            _articleRepository = articleRepository;
            _imageService = imageService;
            _tagService = tagService;
            _httpClientFactory = httpClientFactory;
            _environment = environment;
            _logger = logger;
            _mediaSource = mediaSource;
        }

        public async Task<WordPressImportResultDto> ImportAsync(
            DateTime fromDate,
            DateTime toDate,
            string currentUserId,
            int? take = null,
            CancellationToken cancellationToken = default)
        {
            var result = new WordPressImportResultDto();

            var articles = await _reader.GetArticlesAsync(
                fromDate,
                toDate,
                cancellationToken);

            if (take.HasValue)
            {
                articles = articles
                    .Take(take.Value)
                    .ToList();
            }

            result.Total = articles.Count;

            foreach (var wpArticle in articles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var imported =
                        await ImportArticleAsync(
                            wpArticle,
                            currentUserId,
                            cancellationToken);

                    if (imported)
                        result.Imported++;
                    else
                        result.Skipped++;
                }
                catch (Exception ex)
                {
                    result.Failed++;

                    var message =
                        $"WordPress post {wpArticle.WordPressPostId} " +
                        $"('{wpArticle.PostTitle}') failed: {ex.Message}";

                    result.Errors.Add(message);

                    _logger.LogError(
                        ex,
                        "Failed to import WordPress post {PostId}",
                        wpArticle.WordPressPostId);
                }
            }

            return result;
        }

        private async Task<bool> ImportArticleAsync(
            WordPressArticleImportDto wp,
            string currentUserId,
            CancellationToken cancellationToken)
        {
            var sourceId = wp.WordPressPostId.ToString();

            // ---------------------------------------------------------
            // 1. DUPLICATE CHECK
            // ---------------------------------------------------------

            var existing =
                await _articleRepository.FindBySourceAsync(
                    SourceSystem,
                    sourceId);

            if (existing != null)
            {
                _logger.LogInformation(
                    "Skipping already imported WordPress post {PostId}",
                    wp.WordPressPostId);

                return false;
            }

            // ---------------------------------------------------------
            // 2. RESOLVE AUTHOR
            // ---------------------------------------------------------

            var author =
                await _authorResolver.ResolveAsync(
                    wp.WordPressAuthorId,
                    wp.AuthorName,
                    cancellationToken);

            if (author == null)
            {
                throw new InvalidOperationException(
                    $"Unable to resolve author for WordPress post " +
                    $"{wp.WordPressPostId}.");
            }

            // ---------------------------------------------------------
            // 3. RESOLVE PRIMARY CATEGORY
            // ---------------------------------------------------------

            var category =
                await _categoryResolver.ResolveAsync(
                    wp.WordPressCategoryId,
                    wp.CategoryName,
                    wp.CategorySlug,
                    cancellationToken);

            if (category == null)
            {
                throw new InvalidOperationException(
                    $"Unable to resolve category for WordPress post " +
                    $"{wp.WordPressPostId}.");
            }

            // ---------------------------------------------------------
            // 4. RESOLVE PRIMARY REPORTER
            //
            // B = primary reporter
            // A = first reporter fallback
            // ---------------------------------------------------------

            var reporterInput =
                BuildPrimaryReporterInput(wp);

            var reporters =
                await _reporterResolver.ResolveAsync(
                    reporterInput,
                    cancellationToken);

            var reporter = reporters.FirstOrDefault();

            // Reporter is optional in CMS.
            // Therefore, no exception if WordPress has none.

            // ---------------------------------------------------------
            // 5. SLUG
            // ---------------------------------------------------------

            var slug =
                await GetUniqueSlugAsync(
                    wp.Slug,
                    wp.WordPressPostId);

            // ---------------------------------------------------------
            // 6. CREATE ARTICLE
            // ---------------------------------------------------------

            var article = new Article
            {
                Title = wp.PostTitle?.Trim() ?? string.Empty,

                Slug = slug,

                Summary =
                    string.IsNullOrWhiteSpace(wp.PostSummary)
                        ? wp.PostTitle
                        : wp.PostSummary.Trim(),

                Content =
                    wp.PostContent ?? string.Empty,

                // We don't have separate Yoast title/description
                // in the migration DTO, so use the WordPress title
                // and excerpt as sensible CMS SEO defaults.
                MetaTitle =
                    wp.PostTitle?.Trim() ?? string.Empty,

                MetaDescription =
                    string.IsNullOrWhiteSpace(wp.PostSummary)
                        ? wp.PostTitle?.Trim() ?? string.Empty
                        : wp.PostSummary.Trim(),

                CategoryId = category.Id,

                AuthorId = author.Id,

                ReporterId = reporter?.Id,

                // Preserve WordPress historical dates.
                CreatedAt = wp.PostDate,

                UpdatedAt = wp.PostModifiedDate,

                PublishedAt = wp.PostDate,

                CreatedBy = currentUserId,

                UpdatedBy = currentUserId,

                IsPublished = true,

                IsDeleted = false,

                WorkflowStatus =
                    ArticleWorkflowStatus.Published,

                IsEditorsPick = false,

                EditorialPriority = 0,

                IsFactChecked = false,

                ViewCount = 0,

                // Migration tracking.
                SourceSystem = SourceSystem,

                SourceId = sourceId
            };

            // Save article first because its database ID
            // is required for /uploads/articles/{ArticleId}/.
            var articleId =
                await _articleRepository.AddAsync(article);

            // ---------------------------------------------------------
            // 7. FEATURED IMAGE
            // ---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(wp.FeaturedImagePath))
            {
                try
                {
                    var imagePaths =
                        await DownloadAndSaveFeaturedImageAsync(
                            wp.FeaturedImagePath,
                            articleId,
                            cancellationToken);

                    article.FeaturedImageThumb = imagePaths.thumb;
                    article.FeaturedImageMedium = imagePaths.medium;
                    article.FeaturedImageLarge = imagePaths.large;
                    article.FeaturedImageXl = imagePaths.xl;

                    await _articleRepository.UpdateAsync(article);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Featured image failed for WordPress post {PostId}",
                        wp.WordPressPostId);
                }
            }

            // ---------------------------------------------------------
            // 8. ARTICLE TAGS
            // ---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(wp.PostTags))
            {
                var articleTagsInput =
                    ConvertWordPressTagsToInput(
                        wp.PostTags);

                if (!string.IsNullOrWhiteSpace(
                        articleTagsInput))
                {
                    await _tagService.ReplaceArticleTagsAsync(
                        articleId,
                        articleTagsInput,
                        currentUserId);
                }
            }

            // ---------------------------------------------------------
            // 9. FEATURED IMAGE TAGS + IMAGE TITLE
            // ---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    wp.FeaturedImageTags) ||
                !string.IsNullOrWhiteSpace(
                    wp.FeaturedImageTitle))
            {
                var imageTagsInput =
                    ConvertWordPressTagsToInput(
                        wp.FeaturedImageTags);

                await _tagService.ReplaceFeaturedImageTagsAsync(
                    articleId,
                    imageTagsInput,
                    string.IsNullOrWhiteSpace(
                        wp.FeaturedImageTitle)
                        ? null
                        : wp.FeaturedImageTitle.Trim(),
                    null,
                    null,
                    currentUserId);
            }

            _logger.LogInformation(
                "Imported WordPress post {PostId} as CMS article {ArticleId}",
                wp.WordPressPostId,
                articleId);

            return true;
        }

        private async Task<(
    string thumb,
    string medium,
    string large,
    string xl)>
    DownloadAndSaveFeaturedImageAsync(
        string imageUrl,
        int articleId,
        CancellationToken cancellationToken)
        {
            await using var sourceStream =
                await _mediaSource.OpenAsync(
                    imageUrl,
                    cancellationToken);

            await using var memoryStream =
                new MemoryStream();

            await sourceStream.CopyToAsync(
                memoryStream,
                cancellationToken);

            memoryStream.Position = 0;

            return await _imageService.SaveArticleImagesAsync(
                memoryStream,
                articleId,
                _environment.WebRootPath);
        }

        private async Task<string> GetUniqueSlugAsync(
            string? originalSlug,
            int wordpressPostId)
        {
            var slug = string.IsNullOrWhiteSpace(originalSlug)
                ? $"wordpress-post-{wordpressPostId}"
                : originalSlug.Trim().ToLowerInvariant();

            if (!await _articleRepository.SlugExistsAsync(slug))
                return slug;

            var baseSlug = slug;
            var counter = 1;

            while (await _articleRepository.SlugExistsAsync(slug))
            {
                slug =
                    $"{baseSlug}-wp-{wordpressPostId}";

                if (counter > 1)
                    slug += $"-{counter}";

                counter++;
            }

            return slug;
        }

        private static string? BuildPrimaryReporterInput(
            WordPressArticleImportDto wp)
        {
            // B: WordPress primary reporter.
            if (wp.WordPressPrimaryReporterId.HasValue &&
                !string.IsNullOrWhiteSpace(
                    wp.PrimaryReporterName) &&
                !string.IsNullOrWhiteSpace(
                    wp.PrimaryReporterSlug))
            {
                return string.Join(
                    "|||",
                    wp.WordPressPrimaryReporterId.Value,
                    wp.PrimaryReporterName.Trim(),
                    wp.PrimaryReporterSlug.Trim());
            }

            // A: fallback to first reporter.
            if (string.IsNullOrWhiteSpace(wp.Reporters))
                return null;

            return wp.Reporters
                .Split(
                    "###",
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .FirstOrDefault();
        }

        private static string? ConvertWordPressTagsToInput(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var names = value
                .Split(
                    "###",
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Select(x =>
                {
                    var parts = x.Split(
                        "|||",
                        StringSplitOptions.None);

                    return parts.Length > 0
                        ? parts[0].Trim()
                        : string.Empty;
                })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            // Newline is safer than comma because a tag
            // itself could theoretically contain a comma.
            return string.Join("\n", names);
        }

    }
}
