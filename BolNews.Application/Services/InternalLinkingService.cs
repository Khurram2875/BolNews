using BolNews.Application.Interfaces;
using BolNews.Application.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;
using System.Net;

namespace BolNews.Application.Services
{
    public class InternalLinkingService : IInternalLinkingService
    {
        private readonly IArticleService _articleService;
        private readonly IUrlService _urlService;
        private readonly IMemoryCache _cache;

        public InternalLinkingService(
            IArticleService articleService,
            IUrlService urlService, IMemoryCache cache)
        {
            _articleService = articleService;
            _urlService = urlService;
            _cache = cache;
        }

        public async Task<string> InjectInternalLinksAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return content;

            var articles = await _cache.GetOrCreateAsync("internal_link_articles_v1", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await _articleService.GetRecentArticlesAsync(48);
            });

            var baseUrl = _urlService.GetBaseUrl();

            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            // exclude script/style/pre/code and anchor descendants and only non-empty text
            var textNodes = doc.DocumentNode
                .SelectNodes("//text()[normalize-space(.) != '' and not(ancestor::a) and not(ancestor::script) and not(ancestor::style) and not(ancestor::pre) and not(ancestor::code)]");

            if (textNodes == null)
                return content;

            int linksAdded = 0;
            const int maxLinks = 5;

            // iterate articles and try to inject up to maxLinks anchors
            foreach (var article in articles)
            {
                if (linksAdded >= maxLinks) break;
                if (article == null || string.IsNullOrWhiteSpace(article.Slug) || article.Category == null || string.IsNullOrWhiteSpace(article.Category.Slug))
                    continue;

                var url = $"{baseUrl}/news/{article.Category.Slug}/{article.Slug}";

                // avoid adding a link if the URL already exists in current DOM
                if (doc.DocumentNode.InnerHtml.Contains(url, StringComparison.OrdinalIgnoreCase))
                    continue;

                var keywords = (article.Title ?? string.Empty)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 4)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(2);

                foreach (var keyword in keywords)
                {
                    if (linksAdded >= maxLinks) break;

                    var pattern = Regex.Escape(keyword);
                    // Use ToList() because we'll modify DOM while iterating
                    foreach (var node in textNodes.ToList())
                    {
                        if (linksAdded >= maxLinks) break;
                        if (node.ParentNode == null) continue;

                        var text = node.InnerText ?? string.Empty;
                        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                        if (!match.Success) continue;

                        // preserve original matched text's casing
                        var matchedText = text.Substring(match.Index, match.Length);

                        // HTML-encode components to avoid injecting unsafe content
                        var safeUrl = WebUtility.HtmlEncode(url);
                        var safeMatched = WebUtility.HtmlEncode(matchedText);
                        var anchorHtml = $"<a href=\"{safeUrl}\">{safeMatched}</a>";

                        // Build replacement fragment and insert parsed nodes in place of the text node
                        var newHtml = ReplaceFirst(text, matchedText, anchorHtml);

                        var fragDoc = new HtmlDocument();
                        fragDoc.LoadHtml(newHtml);

                        var parent = node.ParentNode;
                        if (parent == null) continue;

                        // Insert frag children before the existing text node, then remove the original text node
                        foreach (var child in fragDoc.DocumentNode.ChildNodes.Reverse())
                        {
                            // Insert in reverse order so final order matches newHtml
                            parent.InsertAfter(child, node);
                        }
                        parent.RemoveChild(node);

                        linksAdded++;
                        break; // move to next keyword/article
                    }
                }
            }

            return doc.DocumentNode.InnerHtml;
        }

        private string ReplaceFirst(string text, string search, string replace)
        {
            var index = text.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return text;

            return text.Substring(0, index)
                 + replace
                 + text.Substring(index + search.Length);
        }
    }
}
