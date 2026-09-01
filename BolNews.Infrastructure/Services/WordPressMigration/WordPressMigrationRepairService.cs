using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Application.Services;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;
using BolNews.Infrastructure.Services.Video;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services.WordPressMigration
{
    public class WordPressMigrationRepairService
    {
        private const string SourceSystem = "WordPress";

        private readonly IWordPressArticleReader _reader;
        private readonly IArticleRepository _articleRepository;
        private readonly IImageService _imageService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<WordPressMigrationRepairService> _logger;
        private readonly WordPressMediaSource _mediaSource;
        private readonly IMediaLibraryService _mediaLibraryService;
        private readonly IVideoThumbnailService _videoThumbnailService;

        public WordPressMigrationRepairService(
            IWordPressArticleReader reader,
            IArticleRepository articleRepository,
            IImageService imageService,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment,
            ILogger<WordPressMigrationRepairService> logger,
            WordPressMediaSource mediaSource, IMediaLibraryService mediaLibraryService, IVideoThumbnailService videoThumbnailService)
        {
            _reader = reader;
            _articleRepository = articleRepository;
            _imageService = imageService;
            _httpClientFactory = httpClientFactory;
            _environment = environment;
            _logger = logger;
            _mediaSource = mediaSource;
            _mediaLibraryService = mediaLibraryService;
            _videoThumbnailService = videoThumbnailService;

        }

        // ============================================================
        // 1. REPAIR MISSING FEATURED IMAGES
        // ============================================================

        public async Task<WordPressRepairResultDto>
            RepairFeaturedImagesAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            var result = new WordPressRepairResultDto();

            // Get only CMS articles that still have no featured image.
            var missingImageArticles =
                await _articleRepository
                    .GetWordPressArticlesWithMissingFeaturedImagesAsync();

            // Read the WordPress source data.
            var wordpressArticles =
                await _reader.GetArticlesAsync(
                    fromDate,
                    toDate,
                    cancellationToken);

            // Create lookup by WordPress post ID.
            var wordpressLookup =
                wordpressArticles.ToDictionary(
                    x => x.WordPressPostId.ToString());

            // Match only the CMS articles that actually need repair.
            var candidates =
                missingImageArticles
                    .Where(article =>
                        article.SourceId != null &&
                        wordpressLookup.ContainsKey(article.SourceId))
                    .Select(article => new
                    {
                        Article = article,
                        WordPress =
                            wordpressLookup[article.SourceId!]
                    })
                    .ToList();

            _logger.LogInformation(
                "Featured image repair diagnostic: " +
                "MissingImageArticles={Missing}, " +
                "WordPressArticles={WordPress}, " +
                "Candidates={Candidates}",
                missingImageArticles.Count,
                wordpressArticles.Count,
                candidates.Count);

            foreach (var article in missingImageArticles.Take(10))
            {
                _logger.LogInformation(
                    "Missing image candidate: CMS ArticleId={ArticleId}, SourceId={SourceId}",
                    article.Id,
                    article.SourceId);
            }

            result.Total = candidates.Count;

            _logger.LogInformation(
                "WordPress featured image repair started. " +
                "Candidates={Count}",
                candidates.Count);


            // ------------------------------------------------------------
            // Process maximum 4 images concurrently.
            // ------------------------------------------------------------

            using var semaphore = new SemaphoreSlim(4);

            var tasks = candidates.Select(async candidate =>
            {
                await semaphore.WaitAsync(cancellationToken);

                try
                {
                    return await RepairSingleFeaturedImageAsync(
                        candidate.Article,
                        candidate.WordPress,
                        cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var repairResults =
                await Task.WhenAll(tasks);

            // ------------------------------------------------------------
            // Update database sequentially.
            // IMPORTANT: don't concurrently use the same EF DbContext.
            // ------------------------------------------------------------

            foreach (var item in repairResults)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!item.Success)
                {
                    result.Failed++;

                    if (!string.IsNullOrWhiteSpace(item.Error))
                        result.Errors.Add(item.Error);

                    continue;
                }

                try
                {
                    item.Article.FeaturedImageThumb =
                        item.Thumb;

                    item.Article.FeaturedImageMedium =
                        item.Medium;

                    item.Article.FeaturedImageLarge =
                        item.Large;

                    item.Article.FeaturedImageXl =
                        item.Xl;

                    await _articleRepository.UpdateAsync(
                        item.Article);

                    result.Repaired++;
                }
                catch (Exception ex)
                {
                    result.Failed++;

                    result.Errors.Add(
                        $"Article {item.Article.Id} / " +
                        $"WP {item.WordPressPostId}: " +
                        $"Database update failed: {ex.Message}");

                    _logger.LogError(
                        ex,
                        "Failed updating featured image paths. " +
                        "ArticleId={ArticleId}",
                        item.Article.Id);
                }
            }

            result.Skipped =
                missingImageArticles.Count -
                candidates.Count;

            _logger.LogInformation(
                "WordPress featured image repair completed. " +
                "Total={Total}, Repaired={Repaired}, " +
                "Skipped={Skipped}, Failed={Failed}",
                result.Total,
                result.Repaired,
                result.Skipped,
                result.Failed);

            return result;
        }


        private async Task<FeaturedImageRepairItem> RepairSingleFeaturedImageAsync(Article article, WordPressArticleImportDto wp, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(
                    wp.FeaturedImagePath))
            {
                return FeaturedImageRepairItem.Failed(
                    article,
                    wp.WordPressPostId,
                    "No WordPress featured image URL.");
            }

            try
            {
                _logger.LogInformation(
                    "Repairing featured image. " +
                    "ArticleId={ArticleId}, " +
                    "WordPressPostId={PostId}",
                    article.Id,
                    wp.WordPressPostId);

                var normalizedImageUrl =
                        NormalizeWordPressImageUrl(
                        wp.FeaturedImagePath);

                _logger.LogInformation(
                    "Featured image URL normalized. " +
                    "ArticleId={ArticleId}, " +
                    "OriginalUrl={OriginalUrl}, " +
                    "NormalizedUrl={NormalizedUrl}",
                    article.Id,
                    wp.FeaturedImagePath,
                    normalizedImageUrl);

                var paths =
                    await DownloadAndSaveFeaturedImageAsync(
                        normalizedImageUrl,
                        article.Id,
                        cancellationToken);

                _logger.LogInformation(
                    "Featured image repaired successfully. " +
                    "ArticleId={ArticleId}, WordPressPostId={PostId}",
                    article.Id,
                    wp.WordPressPostId);

                return FeaturedImageRepairItem.Successful(
                    article,
                    wp.WordPressPostId,
                    paths.thumb,
                    paths.medium,
                    paths.large,
                    paths.xl);
            }
            catch (Exception ex)
            {
                var message =
                    $"Article {article.Id} / " +
                    $"WP {wp.WordPressPostId}: " +
                    $"Image repair failed. " +
                    $"URL={wp.FeaturedImagePath}. " +
                    $"Error={ex.Message}";

                _logger.LogError(
                    ex,
                    "Featured image repair failed. " +
                    "ArticleId={ArticleId}, " +
                    "WordPressPostId={PostId}",
                    article.Id,
                    wp.WordPressPostId);

                return FeaturedImageRepairItem.Failed(
                    article,
                    wp.WordPressPostId,
                    message);
            }
        }


        // ============================================================
        // 2. NORMALIZE CONTENT
        // ============================================================

        //for testing
        public async Task<WordPressRepairResultDto>
    NormalizeContentAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
        {
            var result = new WordPressRepairResultDto();

            var wordpressArticles =
                await _reader.GetArticlesAsync(
                    fromDate,
                    toDate,
                    cancellationToken);

            foreach (var wp in wordpressArticles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var article =
                    await _articleRepository.FindBySourceAsync(
                        "WordPress",
                        wp.WordPressPostId.ToString());

                if (article == null)
                    continue;

                result.Total++;

                if (string.IsNullOrWhiteSpace(wp.PostContent))
                {
                    result.Skipped++;
                    continue;
                }

                try
                {
                    var normalized =
                        NormalizeWordPressHtml(
                            wp.PostContent);

                    if (string.Equals(
                            normalized,
                            article.Content,
                            StringComparison.Ordinal))
                    {
                        result.Skipped++;
                        continue;
                    }

                    article.Content = normalized;

                    await _articleRepository.UpdateAsync(
                        article);

                    result.Repaired++;
                }
                catch (Exception ex)
                {
                    result.Failed++;

                    result.Errors.Add(
                        $"Article {article.Id} / " +
                        $"WP {wp.WordPressPostId}: " +
                        ex.Message);

                    _logger.LogError(
                        ex,
                        "Content normalization failed. " +
                        "ArticleId={ArticleId}, WP={WordPressPostId}",
                        article.Id,
                        wp.WordPressPostId);
                }
            }

            return result;
        }

        //original
        //public async Task<WordPressRepairResultDto>
        //    NormalizeContentAsync(
        //        DateTime fromDate,
        //        DateTime toDate,
        //        CancellationToken cancellationToken = default)
        //{
        //    var result = new WordPressRepairResultDto();

        //    var wordpressArticles =
        //        await _reader.GetArticlesAsync(
        //            fromDate,
        //            toDate,
        //            cancellationToken);

        //    foreach (var wp in wordpressArticles)
        //    {
        //        cancellationToken.ThrowIfCancellationRequested();

        //        var article =
        //            await _articleRepository.FindBySourceAsync(
        //                SourceSystem,
        //                wp.WordPressPostId.ToString());

        //        if (article == null)
        //            continue;

        //        result.Total++;

        //        if (string.IsNullOrWhiteSpace(wp.PostContent))
        //        {
        //            result.Skipped++;
        //            continue;
        //        }

        //        try
        //        {
        //            var normalized =
        //                NormalizeWordPressHtml(
        //                    wp.PostContent);

        //            if (string.Equals(
        //                normalized,
        //                article.Content,
        //                StringComparison.Ordinal))
        //            {
        //                result.Skipped++;
        //                continue;
        //            }

        //            article.Content = normalized;

        //            await _articleRepository.UpdateAsync(article);

        //            result.Repaired++;
        //        }
        //        catch (Exception ex)
        //        {
        //            result.Failed++;

        //            result.Errors.Add(
        //                $"Article {article.Id} / WP {wp.WordPressPostId}: " +
        //                ex.Message);

        //            _logger.LogError(
        //                ex,
        //                "Content normalization failed for ArticleId={ArticleId}",
        //                article.Id);
        //        }
        //    }

        //    return result;
        //}

        // ============================================================
        // 3. INLINE MEDIA
        // ============================================================

        public async Task<WordPressRepairResultDto>
    ProcessInlineMediaAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
        {
            var result = new WordPressRepairResultDto();

            var wordpressArticles =
                await _reader.GetArticlesAsync(
                    fromDate,
                    toDate,
                    cancellationToken);

            // for testing, filter to a specific WordPress post ID
            //wordpressArticles = wordpressArticles
            //    .Where(x => x.WordPressPostId == 1165024)
            //    .ToList();

            _logger.LogInformation(
                "Inline media processing started. WordPressArticles={Count}",
                wordpressArticles.Count);

            foreach (var wp in wordpressArticles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var article =
                    await _articleRepository.FindBySourceAsync(
                        SourceSystem,
                        wp.WordPressPostId.ToString());

                if (article == null)
                    continue;

                result.Total++;

                if (string.IsNullOrWhiteSpace(article.Content))
                {
                    result.Skipped++;
                    continue;
                }

                try
                {
                    var processed =
                        await ProcessContentMediaAsync(
                            article.Content,
                            article.Id,
                            cancellationToken);

                    if (!processed.Changed)
                    {
                        result.Skipped++;
                        continue;
                    }

                    article.Content = processed.Content;

                    await _articleRepository.UpdateAsync(article);

                    // Count articles repaired, not individual images.
                    result.Repaired++;

                    _logger.LogInformation(
                        "Inline media processed. " +
                        "ArticleId={ArticleId}, " +
                        "WordPressPostId={WordPressPostId}, " +
                        "ImagesProcessed={ImagesProcessed}",
                        article.Id,
                        wp.WordPressPostId,
                        processed.ImagesProcessed);
                }
                catch (Exception ex)
                {
                    result.Failed++;

                    result.Errors.Add(
                        $"Article {article.Id} / WP {wp.WordPressPostId}: " +
                        $"Inline media processing failed: {ex.Message}");

                    _logger.LogError(
                        ex,
                        "Inline media processing failed. ArticleId={ArticleId}",
                        article.Id);
                }
            }

            _logger.LogInformation(
                "Inline media processing completed. " +
                "Total={Total}, Repaired={Repaired}, " +
                "Skipped={Skipped}, Failed={Failed}",
                result.Total,
                result.Repaired,
                result.Skipped,
                result.Failed);

            return result;
        }

        // ============================================================
        // FEATURED IMAGE DOWNLOAD
        // ============================================================

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
            Exception? lastException = null;

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                try
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

                    return await _imageService
                        .SaveArticleImagesAsync(
                            memoryStream,
                            articleId,
                            _environment.WebRootPath);
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (attempt == 1)
                    {
                        _logger.LogWarning(
                            "Featured image attempt 1 failed. " +
                            "ArticleId={ArticleId}, URL={Url}. " +
                            "Retrying once...",
                            articleId,
                            imageUrl);
                    }
                }
            }

            throw new InvalidOperationException(
                $"Unable to load/process featured image " +
                $"after 2 attempts: {imageUrl}",
                lastException);
        }

        // ============================================================
        // HTML NORMALIZATION
        // ============================================================

        //Temporary 
        private static string NormalizeWordPressHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            html = html.Replace("\r\n", "\n");

            var blocks =
                Regex.Split(
                    html.Trim(),
                    @"\n\s*\n");

            var result = new StringBuilder();

            foreach (var block in blocks)
            {
                var content = block.Trim();

                if (string.IsNullOrWhiteSpace(content))
                    continue;

                if (Regex.IsMatch(
                        content,
                        @"^\s*<(p|h[1-6]|ul|ol|blockquote|figure|div|table|iframe)\b",
                        RegexOptions.IgnoreCase))
                {
                    result.AppendLine(content);
                    continue;
                }

                result.Append("<p>");
                result.Append(content);
                result.AppendLine("</p>");
            }

            return result.ToString().Trim();
        }

        //original
        //private static string NormalizeWordPressHtml(string html)
        //{
        //    if (string.IsNullOrWhiteSpace(html))
        //        return string.Empty;

        //    var document = new HtmlDocument();
        //    document.LoadHtml(html);

        //    var root = document.DocumentNode;

        //    // ------------------------------------------------------------
        //    // Remove comments
        //    // ------------------------------------------------------------

        //    var comments = root.SelectNodes("//comment()");

        //    if (comments != null)
        //    {
        //        foreach (var comment in comments.ToList())
        //            comment.Remove();
        //    }

        //    // ------------------------------------------------------------
        //    // Remove empty nodes that have no meaningful content.
        //    // ------------------------------------------------------------

        //    var emptyDivs = root.SelectNodes(
        //        "//div[not(descendant::img) and not(descendant::iframe) and not(normalize-space(string(.)))]");

        //    if (emptyDivs != null)
        //    {
        //        foreach (var div in emptyDivs.ToList())
        //            div.Remove();
        //    }

        //    // ------------------------------------------------------------
        //    // Convert DIV blocks to P where appropriate.
        //    //
        //    // WordPress/editor-generated content frequently uses:
        //    //
        //    // <div><strong>Text...</strong></div>
        //    //
        //    // CMS/Quill expects:
        //    //
        //    // <p><strong>Text...</strong></p>
        //    // ------------------------------------------------------------

        //    var divs = root.SelectNodes("//div");

        //    if (divs != null)
        //    {
        //        foreach (var div in divs.ToList())
        //        {
        //            // Don't convert social/embed containers.
        //            var className =
        //                div.GetAttributeValue("class", "");

        //            if (className.Contains(
        //                    "quill-social-embed",
        //                    StringComparison.OrdinalIgnoreCase))
        //            {
        //                continue;
        //            }

        //            // Don't convert DIVs containing block-level structures.
        //            var hasBlockChildren =
        //                div.SelectSingleNode(
        //                    "./p | ./div | ./section | ./article | ./ul | ./ol | ./table | ./blockquote | ./figure | ./iframe")
        //                != null;

        //            if (hasBlockChildren)
        //                continue;

        //            var paragraph =
        //                HtmlNode.CreateNode("<p></p>");

        //            foreach (var child in div.ChildNodes.ToList())
        //            {
        //                child.Remove();
        //                paragraph.AppendChild(child);
        //            }

        //            div.ParentNode?.ReplaceChild(
        //                paragraph,
        //                div);
        //        }
        //    }

        //    // ------------------------------------------------------------
        //    // Convert WordPress figures containing images to P.
        //    // ------------------------------------------------------------

        //    var figures =
        //        root.SelectNodes("//figure");

        //    if (figures != null)
        //    {
        //        foreach (var figure in figures.ToList())
        //        {
        //            var image =
        //                figure.SelectSingleNode(".//img");

        //            if (image == null)
        //                continue;

        //            var paragraph =
        //                HtmlNode.CreateNode("<p></p>");

        //            image.Remove();

        //            paragraph.AppendChild(image);

        //            // Preserve figcaption as a paragraph.
        //            var caption =
        //                figure.SelectSingleNode(".//figcaption");

        //            if (caption != null)
        //            {
        //                var captionParagraph =
        //                    HtmlNode.CreateNode("<p></p>");

        //                caption.Remove();

        //                captionParagraph.AppendChild(caption);

        //                paragraph.AppendChild(
        //                    captionParagraph);
        //            }

        //            figure.ParentNode?.ReplaceChild(
        //                paragraph,
        //                figure);
        //        }
        //    }

        //    // ------------------------------------------------------------
        //    // Remove editor-specific attributes.
        //    //
        //    // Examples:
        //    // data-start
        //    // data-end
        //    // data-turn-id
        //    // data-is-intersecting
        //    // data-testid
        //    // etc.
        //    // ------------------------------------------------------------

        //    var allNodes =
        //        root.SelectNodes("//*");

        //    if (allNodes != null)
        //    {
        //        foreach (var node in allNodes.ToList())
        //        {
        //            foreach (var attribute in node.Attributes.ToList())
        //            {
        //                var name =
        //                    attribute.Name;

        //                if (name.StartsWith(
        //                        "data-",
        //                        StringComparison.OrdinalIgnoreCase))
        //                {
        //                    node.Attributes.Remove(
        //                        attribute.Name);
        //                }
        //            }
        //        }
        //    }

        //    // ------------------------------------------------------------
        //    // Clean WordPress/editor classes.
        //    // ------------------------------------------------------------

        //    var elementsWithClass =
        //        root.SelectNodes("//*[@class]");

        //    if (elementsWithClass != null)
        //    {
        //        foreach (var element in elementsWithClass.ToList())
        //        {
        //            var classValue =
        //                element.GetAttributeValue(
        //                    "class",
        //                    "");

        //            if (string.IsNullOrWhiteSpace(classValue))
        //                continue;

        //            var classes =
        //                classValue
        //                    .Split(
        //                        ' ',
        //                        StringSplitOptions.RemoveEmptyEntries)
        //                    .ToList();

        //            var cleanedClasses =
        //                new List<string>();

        //            foreach (var cls in classes)
        //            {
        //                // WordPress alignment → Quill alignment.
        //                if (cls.Equals(
        //                        "aligncenter",
        //                        StringComparison.OrdinalIgnoreCase))
        //                {
        //                    cleanedClasses.Add(
        //                        "ql-align-center");

        //                    continue;
        //                }

        //                if (cls.Equals(
        //                        "alignright",
        //                        StringComparison.OrdinalIgnoreCase))
        //                {
        //                    cleanedClasses.Add(
        //                        "ql-align-right");

        //                    continue;
        //                }

        //                if (cls.Equals(
        //                        "alignleft",
        //                        StringComparison.OrdinalIgnoreCase))
        //                {
        //                    cleanedClasses.Add(
        //                        "ql-align-left");

        //                    continue;
        //                }

        //                if (cls.Equals(
        //                        "alignjustify",
        //                        StringComparison.OrdinalIgnoreCase))
        //                {
        //                    cleanedClasses.Add(
        //                        "ql-align-justify");

        //                    continue;
        //                }

        //                // Keep Quill classes only.
        //                if (cls.StartsWith(
        //                        "ql-",
        //                        StringComparison.OrdinalIgnoreCase))
        //                {
        //                    cleanedClasses.Add(cls);
        //                }
        //            }

        //            if (cleanedClasses.Count == 0)
        //                element.Attributes.Remove("class");
        //            else
        //                element.SetAttributeValue(
        //                    "class",
        //                    string.Join(
        //                        " ",
        //                        cleanedClasses.Distinct()));
        //        }
        //    }

        //    // ------------------------------------------------------------
        //    // Remove unnecessary inline styles.
        //    //
        //    // Keep styles only where they are actually useful.
        //    // ------------------------------------------------------------

        //    var styledNodes =
        //        root.SelectNodes("//*[@style]");

        //    if (styledNodes != null)
        //    {
        //        foreach (var node in styledNodes.ToList())
        //        {
        //            var style =
        //                node.GetAttributeValue(
        //                    "style",
        //                    "");

        //            // Remove editor-generated font-size: inherit,
        //            // unnecessary WordPress formatting, etc.
        //            style = Regex.Replace(
        //                style,
        //                @"font-size\s*:\s*inherit\s*;?",
        //                "",
        //                RegexOptions.IgnoreCase);

        //            style = Regex.Replace(
        //                style,
        //                @"\s*;\s*$",
        //                "");

        //            if (string.IsNullOrWhiteSpace(style))
        //                node.Attributes.Remove("style");
        //            else
        //                node.SetAttributeValue(
        //                    "style",
        //                    style);
        //        }
        //    }

        //    // ------------------------------------------------------------
        //    // Clean image attributes.
        //    //
        //    // Keep:
        //    // src
        //    // alt
        //    // width
        //    // height
        //    // title
        //    // ------------------------------------------------------------

        //    var images =
        //        root.SelectNodes("//img");

        //    if (images != null)
        //    {
        //        foreach (var image in images.ToList())
        //        {
        //            var allowed =
        //                new HashSet<string>(
        //                    StringComparer.OrdinalIgnoreCase)
        //                {
        //            "src",
        //            "alt",
        //            "width",
        //            "height",
        //            "title"
        //                };

        //            foreach (var attribute in
        //                     image.Attributes.ToList())
        //            {
        //                if (!allowed.Contains(
        //                        attribute.Name))
        //                {
        //                    image.Attributes.Remove(
        //                        attribute.Name);
        //                }
        //            }

        //            var src =
        //                image.GetAttributeValue(
        //                    "src",
        //                    "");

        //            if (string.IsNullOrWhiteSpace(src))
        //                image.Remove();
        //        }
        //    }

        //    // ------------------------------------------------------------
        //    // Remove empty paragraphs.
        //    // ------------------------------------------------------------

        //    var paragraphs =
        //        root.SelectNodes("//p");

        //    if (paragraphs != null)
        //    {
        //        foreach (var paragraph in paragraphs.ToList())
        //        {
        //            var hasImage =
        //                paragraph.SelectSingleNode(".//img") != null;

        //            var hasIframe =
        //                paragraph.SelectSingleNode(".//iframe") != null;

        //            var text =
        //                HtmlEntity.DeEntitize(
        //                    paragraph.InnerText)
        //                .Trim();

        //            if (!hasImage &&
        //                !hasIframe &&
        //                string.IsNullOrWhiteSpace(text))
        //            {
        //                paragraph.Remove();
        //            }
        //        }
        //    }

        //    // ------------------------------------------------------------
        //    // Normalize excessive whitespace between blocks.
        //    // ------------------------------------------------------------

        //    var result =
        //        root.InnerHtml.Trim();

        //    result =
        //        Regex.Replace(
        //            result,
        //            @"\r?\n\s*\r?\n\s*\r?\n+",
        //            "\r\n\r\n");

        //    return result;
        //}

        // ============================================================
        // INLINE IMAGE / EMBED PROCESSING
        // ============================================================

        private async Task<(
    string Content,
    bool Changed,
    int ImagesProcessed)>
    ProcessContentMediaAsync(
        string html,
        int articleId,
        CancellationToken cancellationToken)
        {
            var document = new HtmlDocument();

            document.LoadHtml(html);

            var changed = false;
            var imagesProcessed = 0;

            // ============================================================
            // INLINE IMAGES
            // ============================================================

            var images =
                document.DocumentNode.SelectNodes("//img");

            if (images != null)
            {
                foreach (var image in images.ToList())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var src =
                        image.GetAttributeValue(
                            "src",
                            string.Empty);

                    if (string.IsNullOrWhiteSpace(src))
                        continue;

                    if (!IsWordPressImage(src))
                        continue;

                    try
                    {
                        // IMPORTANT:
                        // Convert legacy WordPress host/IP to bolnews.com
                        // before downloading.
                        var normalizedUrl =
                            NormalizeWordPressImageUrl(src);

                        _logger.LogInformation(
                            "Processing inline image. " +
                            "ArticleId={ArticleId}, " +
                            "OriginalUrl={OriginalUrl}, " +
                            "NormalizedUrl={NormalizedUrl}",
                            articleId,
                            src,
                            normalizedUrl);

                        var localUrl =
                            await DownloadInlineImageAsync(
                                normalizedUrl,
                                articleId,
                                cancellationToken);

                        image.SetAttributeValue(
                            "src",
                            localUrl);

                        // Remove WordPress-specific image attributes.
                        image.Attributes.Remove("srcset");
                        image.Attributes.Remove("sizes");
                        image.Attributes.Remove("loading");
                        image.Attributes.Remove("decoding");
                        image.Attributes.Remove("class");
                        image.Attributes.Remove("style");

                        changed = true;
                        imagesProcessed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Inline image download failed. " +
                            "ArticleId={ArticleId}, URL={Url}",
                            articleId,
                            src);

                        // Keep original URL if download fails.
                        // We do not destroy article content.
                    }
                }
            }

            // ============================================================
            // IFRAME / EMBEDS
            // ============================================================

            var iframes =
                document.DocumentNode.SelectNodes("//iframe");

            if (iframes != null)
            {
                foreach (var iframe in iframes.ToList())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var src =
                        iframe.GetAttributeValue(
                            "src",
                            string.Empty);

                    if (string.IsNullOrWhiteSpace(src))
                        continue;

                    if (!IsAllowedEmbed(src))
                    {
                        // Preserve unsupported embeds.
                        continue;
                    }

                    var parent =
                        iframe.ParentNode;

                    if (parent == null)
                        continue;

                    var parentClasses =
                        parent.GetAttributeValue(
                            "class",
                            "")
                        .Split(
                            ' ',
                            StringSplitOptions.RemoveEmptyEntries);

                    var alreadyWrapped =
                        parent.Name.Equals(
                            "div",
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        parentClasses.Contains(
                            "quill-social-embed",
                            StringComparer.OrdinalIgnoreCase);

                    if (!alreadyWrapped)
                    {
                        var wrapper =
                            HtmlNode.CreateNode(
                                "<div class=\"quill-social-embed\"></div>");

                        iframe.Remove();

                        wrapper.AppendChild(iframe);

                        parent.AppendChild(wrapper);

                        changed = true;
                    }
                }
            }

            return (
                document.DocumentNode.InnerHtml.Trim(),
                changed,
                imagesProcessed);
        }

        private async Task<string> DownloadInlineImageAsync(
    string imageUrl,
    int articleId,
    CancellationToken cancellationToken)
        {
            var client =
                _httpClientFactory.CreateClient(
                    "WordPressMedia");

            using var response =
                await client.GetAsync(
                    imageUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var sourceStream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            await using var memoryStream =
                new MemoryStream();

            await sourceStream.CopyToAsync(
                memoryStream,
                cancellationToken);

            memoryStream.Position = 0;

            var fileName =
                $"{Guid.NewGuid():N}.webp";

            return await _imageService
                .SaveArticleContentImageAsync(
                    memoryStream,
                    fileName,
                    _environment.WebRootPath);
        }

        private static bool IsWordPressImage(
            string url)
        {
            return
                url.Contains(
                    "wp-content/uploads",
                    StringComparison.OrdinalIgnoreCase)
                ||
                url.Contains(
                    "/uploads/",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAllowedEmbed(
            string url)
        {
            if (!Uri.TryCreate(
                    url,
                    UriKind.Absolute,
                    out var uri))
            {
                return false;
            }

            var host =
                uri.Host.ToLowerInvariant();

            return
                host.Contains("youtube.com") ||
                host.Contains("youtube-nocookie.com") ||
                host.Contains("youtu.be") ||
                host.Contains("instagram.com") ||
                host.Contains("facebook.com") ||
                host.Contains("fb.watch") ||
                host.Contains("tiktok.com") ||
                host.Contains("twitter.com") ||
                host.Contains("x.com");
        }
        // Add this method to the WordPressMigrationRepairService class

        /// <summary>
        /// Normalizes a WordPress image URL by trimming whitespace and ensuring it is a valid absolute URL.
        /// </summary>
        private static string NormalizeWordPressImageUrl(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return imageUrl;

            if (!Uri.TryCreate(
                    imageUrl,
                    UriKind.Absolute,
                    out var uri))
            {
                return imageUrl;
            }

            var builder = new UriBuilder(uri)
            {
                Host = "bolnews.com"
            };

            return builder.Uri.ToString();
        }
        public async Task<WordPressRepairResultDto>
    ProcessWordPressVideosAsync(
        DateTime fromDate,
        DateTime toDate,
        string currentUserId,
        CancellationToken cancellationToken = default)
        {
            var result =
                new WordPressRepairResultDto();

            var wordpressArticles =
                await _reader.GetArticlesAsync(
                    fromDate,
                    toDate,
                    cancellationToken);

            var cmsArticles =
                await _articleRepository
                    .GetWordPressArticlesWithMissingFeaturedImagesAsync();

            var wordpressLookup =
                cmsArticles
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.SourceId))
                    .ToDictionary(
                        x => x.SourceId!,
                        x => x);

            foreach (var wp in wordpressArticles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(
                        wp.PostContent))
                {
                    continue;
                }

                if (!wordpressLookup.TryGetValue(
                        wp.WordPressPostId.ToString(),
                        out var article))
                {
                    continue;
                }

                var videoUrls =
                    ExtractLocalMp4Urls(
                        wp.PostContent);

                if (videoUrls.Count == 0)
                    continue;

                result.Total += videoUrls.Count;

                foreach (var videoUrl in videoUrls)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var mediaUrl =
                            await MigrateSingleWordPressVideoAsync(
                                article,
                                videoUrl,
                                currentUserId,
                                cancellationToken);

                        article.Content =
                            article.Content.Replace(
                                videoUrl,
                                mediaUrl,
                                StringComparison.OrdinalIgnoreCase);

                        await _articleRepository.UpdateAsync(
                            article);

                        result.Repaired++;
                    }
                    catch (Exception ex)
                    {
                        result.Failed++;

                        result.Errors.Add(
                            $"Article {article.Id} / " +
                            $"WP {wp.WordPressPostId}: " +
                            $"Video migration failed. " +
                            $"URL={videoUrl}. " +
                            $"Error={ex.Message}");

                        _logger.LogError(
                            ex,
                            "WordPress video migration failed. " +
                            "ArticleId={ArticleId}, URL={Url}",
                            article.Id,
                            videoUrl);
                    }
                }
            }

            return result;
        }
        private static List<string> ExtractLocalMp4Urls(
    string html)
        {
            var results =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            var pattern =
                @"(?i)(?:https?:\/\/[^""'\s<>]+|\/[^""'\s<>]+)"
                + @"\.mp4(?:\?[^""'\s<>]*)?";

            foreach (Match match in
                Regex.Matches(html, pattern))
            {
                var url =
                    match.Value;

                if (url.Contains(
                        "/wp-content/uploads/",
                        StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(url);
                }
            }

            return results.ToList();
        }
        private async Task<string>
    MigrateSingleWordPressVideoAsync(
        Article article,
        string videoUrl,
        string currentUserId,
        CancellationToken cancellationToken)
        {
            var normalizedUrl =
                NormalizeWordPressImageUrl(videoUrl);

            await using var sourceStream =
                await _mediaSource.OpenAsync(
                    normalizedUrl,
                    cancellationToken);

            await using var memoryStream =
                new MemoryStream();

            await sourceStream.CopyToAsync(
                memoryStream,
                cancellationToken);

            memoryStream.Position = 0;

            var originalFileName =
                Path.GetFileName(
                    new Uri(normalizedUrl)
                        .AbsolutePath);

            if (string.IsNullOrWhiteSpace(originalFileName))
            {
                originalFileName =
                    $"video-{Guid.NewGuid():N}.mp4";
            }

            // ------------------------------------------------------------
            // Create MediaAsset first.
            // ------------------------------------------------------------

            var mediaId =
                await _mediaLibraryService.CreateAsync(
                    new MediaAssetDto
                    {
                        MediaType = MediaType.Video,
                        OriginalFileName = originalFileName,
                        AltText = originalFileName
                    },
                    currentUserId);

            var mediaDirectory =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "media",
                    mediaId.ToString());

            Directory.CreateDirectory(
                mediaDirectory);

            var videoFilePath =
                Path.Combine(
                    mediaDirectory,
                    "original.mp4");

            var thumbnailFilePath =
                Path.Combine(
                    mediaDirectory,
                    $"video-{mediaId}.jpg");

            // ------------------------------------------------------------
            // Save MP4.
            // ------------------------------------------------------------

            memoryStream.Position = 0;

            await using (var fileStream =
                new FileStream(
                    videoFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    1024 * 64,
                    useAsync: true))
            {
                await memoryStream.CopyToAsync(
                    fileStream,
                    cancellationToken);
            }

            // ------------------------------------------------------------
            // Generate thumbnail with FFmpeg.
            // ------------------------------------------------------------

            await _videoThumbnailService
                .GenerateThumbnailAsync(
                    videoFilePath,
                    thumbnailFilePath,
                    cancellationToken);

            var videoUrlPath =
                $"/uploads/media/{mediaId}/original.mp4";

            var thumbnailUrlPath =
                $"/uploads/media/{mediaId}/video-{mediaId}.jpg";

            // ------------------------------------------------------------
            // Update MediaAsset storage.
            // ------------------------------------------------------------

            await _mediaLibraryService.SetStorageAsync(
                mediaId,
                videoUrlPath,
                thumbnailUrlPath,
                null,
                null,
                currentUserId);

            return videoUrlPath;
        }
        private sealed class FeaturedImageRepairItem
        {
            public Article Article { get; init; } = null!;

            public int WordPressPostId { get; init; }

            public bool Success { get; init; }

            public string? Thumb { get; init; }

            public string? Medium { get; init; }

            public string? Large { get; init; }

            public string? Xl { get; init; }

            public string? Error { get; init; }

            public static FeaturedImageRepairItem Successful(
                Article article,
                int wordpressPostId,
                string thumb,
                string medium,
                string large,
                string xl)
            {
                return new FeaturedImageRepairItem
                {
                    Article = article,
                    WordPressPostId = wordpressPostId,
                    Success = true,
                    Thumb = thumb,
                    Medium = medium,
                    Large = large,
                    Xl = xl
                };
            }

            public static FeaturedImageRepairItem Failed(
                Article article,
                int wordpressPostId,
                string error)
            {
                return new FeaturedImageRepairItem
                {
                    Article = article,
                    WordPressPostId = wordpressPostId,
                    Success = false,
                    Error = error
                };
            }
            
        }
    }

}
