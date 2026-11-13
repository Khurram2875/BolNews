using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BolNews.Infrastructure.Services.WordPressMigration
{
    /// <summary>
    /// Reads published posts from a WordPress site through /wp-json/wp/v2/posts and maps them to the same DTO the
    /// database reader produces, so the existing import pipeline (authors, categories, tags, images, de-duplication)
    /// is reused unchanged.
    /// </summary>
    public sealed class WordPressRestArticleReader : IWordPressArticleReader
    {
        public const string HttpClientName = "WordPressRest";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly WordPressRestOptions _options;
        private readonly ILogger<WordPressRestArticleReader> _logger;

        public WordPressRestArticleReader(
            IHttpClientFactory httpClientFactory,
            IOptions<WordPressRestOptions> options,
            ILogger<WordPressRestArticleReader> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<List<WordPressArticleImportDto>> GetArticlesAsync(
            DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
                throw new InvalidOperationException("WordPressRest:BaseUrl is not configured.");

            var baseUrl = _options.BaseUrl.TrimEnd('/');
            var perPage = Math.Clamp(_options.PerPage, 1, 100);
            var client = _httpClientFactory.CreateClient(HttpClientName);

            if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.ApplicationPassword))
            {
                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Username}:{_options.ApplicationPassword}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
            }

            var result = new List<WordPressArticleImportDto>();
            var totalPages = 1;

            for (var page = 1; page <= totalPages && page <= _options.MaxPages; page++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var url = $"{baseUrl}/wp-json/wp/v2/posts?_embed=1&status=publish&orderby=date&order=asc" +
                          $"&per_page={perPage}&page={page}" +
                          $"&after={Uri.EscapeDataString(fromDate.ToString("s", CultureInfo.InvariantCulture))}" +
                          $"&before={Uri.EscapeDataString(toDate.ToString("s", CultureInfo.InvariantCulture))}";

                using var response = await client.GetAsync(url, cancellationToken);

                // WordPress answers 400 "rest_post_invalid_page_number" when we ask beyond the last page.
                if (response.StatusCode == HttpStatusCode.BadRequest && page > 1) break;
                response.EnsureSuccessStatusCode();

                if (response.Headers.TryGetValues("X-WP-TotalPages", out var values) &&
                    int.TryParse(values.FirstOrDefault(), out var pages))
                    totalPages = pages;

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                foreach (var post in doc.RootElement.EnumerateArray())
                {
                    try
                    {
                        var mapped = Map(post);
                        if (string.IsNullOrWhiteSpace(mapped.AuthorName)) mapped.AuthorName = _options.DefaultAuthorName;
                        result.Add(mapped);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "WORDPRESS_REST could not map post {Id}", GetInt(post, "id"));
                    }
                }

                _logger.LogInformation("WORDPRESS_REST fetched page {Page}/{Total} ({Count} posts so far)", page, totalPages, result.Count);
            }

            return result;
        }

        // ------------------------------------------------------------------ mapping

        private static WordPressArticleImportDto Map(JsonElement post)
        {
            var dto = new WordPressArticleImportDto
            {
                WordPressPostId = GetInt(post, "id") ?? throw new InvalidOperationException("Post has no id."),
                PostDate = ParseUtc(post, "date_gmt", "date"),
                PostModifiedDate = ParseUtc(post, "modified_gmt", "modified"),
                PostTitle = CleanText(Rendered(post, "title")),
                PostSummary = NullIfEmpty(CleanText(Rendered(post, "excerpt"))),
                PostContent = CleanContent(Rendered(post, "content")),
                Slug = SafeDecode(GetString(post, "slug")),
                PostStatus = GetString(post, "status"),
                PostType = GetString(post, "type"),
                WordPressAuthorId = GetInt(post, "author")
            };

            var embedded = post.TryGetProperty("_embedded", out var e) ? e : default;

            // Author
            if (embedded.ValueKind == JsonValueKind.Object &&
                embedded.TryGetProperty("author", out var authors) && authors.ValueKind == JsonValueKind.Array && authors.GetArrayLength() > 0)
                dto.AuthorName = CleanText(GetString(authors[0], "name"));

            // Terms: wp:term[0] = categories, wp:term[1] = tags (when the taxonomies are exposed to REST)
            var categories = new List<(int id, string name, string slug)>();
            var tags = new List<(string name, string slug)>();
            if (embedded.ValueKind == JsonValueKind.Object &&
                embedded.TryGetProperty("wp:term", out var terms) && terms.ValueKind == JsonValueKind.Array)
            {
                foreach (var group in terms.EnumerateArray())
                {
                    if (group.ValueKind != JsonValueKind.Array) continue;
                    foreach (var term in group.EnumerateArray())
                    {
                        var taxonomy = GetString(term, "taxonomy");
                        var name = CleanText(GetString(term, "name"));
                        var slug = SafeDecode(GetString(term, "slug"));
                        if (taxonomy == "category") categories.Add((GetInt(term, "id") ?? 0, name, slug));
                        else if (taxonomy == "post_tag") tags.Add((name, slug));
                    }
                }
            }

            // BOL URLs look like /pakistan/861600/: the first path segment is the primary category.
            // Prefer it over "latest-news"/"video" style secondary categories.
            var firstSegment = FirstPathSegment(GetString(post, "link"));
            var primary = categories.FirstOrDefault(c => string.Equals(c.slug, firstSegment, StringComparison.OrdinalIgnoreCase));
            if (primary.id == 0) primary = categories.FirstOrDefault(c => c.slug != "latest-news");
            if (primary.id == 0 && categories.Count > 0) primary = categories[0];
            if (primary.id != 0)
            {
                dto.WordPressCategoryId = primary.id;
                dto.CategoryName = primary.name;
                dto.CategorySlug = primary.slug;
            }

            if (tags.Count > 0)
                dto.PostTags = string.Join("###", tags.Select(t => $"{t.name}|||{t.slug}"));

            // Featured image
            if (embedded.ValueKind == JsonValueKind.Object &&
                embedded.TryGetProperty("wp:featuredmedia", out var media) && media.ValueKind == JsonValueKind.Array && media.GetArrayLength() > 0)
            {
                var m = media[0];
                dto.FeaturedImageId = GetInt(m, "id");
                dto.FeaturedImagePath = NullIfEmpty(GetString(m, "source_url"));
                dto.FeaturedImageTitle = NullIfEmpty(CleanText(Rendered(m, "title")));
            }

            // Yoast SEO fields (title/description) when the plugin exposes them.
            if (post.TryGetProperty("yoast_head_json", out var yoast) && yoast.ValueKind == JsonValueKind.Object)
            {
                // Many sites hide /users from the REST API, so the embedded author has no name. Yoast still reports it.
                if (string.IsNullOrWhiteSpace(dto.AuthorName))
                    dto.AuthorName = NullIfEmpty(CleanText(GetString(yoast, "author")));
                dto.SeoTitle = NullIfEmpty(CleanText(GetString(yoast, "title")));
                dto.SeoDescription = NullIfEmpty(CleanText(GetString(yoast, "description")));
            }

            return dto;
        }

        // ------------------------------------------------------------------ helpers

        private static string Rendered(JsonElement parent, string name) =>
            parent.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Object ? GetString(p, "rendered") : "";

        private static string GetString(JsonElement e, string name) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "";

        private static int? GetInt(JsonElement e, string name) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var i) ? i : null;

        private static DateTime ParseUtc(JsonElement post, string gmtName, string localName)
        {
            var text = GetString(post, gmtName);
            if (string.IsNullOrEmpty(text)) text = GetString(post, localName);
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d)
                ? d : DateTime.UtcNow;
        }

        private static string CleanText(string? html)
        {
            var text = WebUtility.HtmlDecode(Regex.Replace(html ?? "", "<[^>]+>", " ")).Replace('\u00A0', ' ');
            return Regex.Replace(text, @"\s+", " ").Trim();
        }

        /// <summary>Removes script/style blocks and Elementor/WordPress wrappers that mean nothing outside WordPress.</summary>
        private static string CleanContent(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";
            html = Regex.Replace(html, @"<(script|style|noscript)\b[^>]*>.*?</\1>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<!--.*?-->", "", RegexOptions.Singleline);
            html = Regex.Replace(html, @"\s(data-elementor-[\w-]+|data-widget_type|data-id)=""[^""]*""", "", RegexOptions.IgnoreCase);
            return html.Trim();
        }

        private static string SafeDecode(string value)
        {
            try { return Uri.UnescapeDataString(value ?? ""); } catch { return value ?? ""; }
        }

        private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

        private static string FirstPathSegment(string link)
        {
            if (!Uri.TryCreate(link, UriKind.Absolute, out var uri)) return "";
            return SafeDecode(uri.AbsolutePath.Trim('/').Split('/').FirstOrDefault() ?? "");
        }
    }
}
