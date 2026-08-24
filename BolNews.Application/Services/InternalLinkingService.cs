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
        //temporarily changed the method to v2, which uses a more efficient approach to avoid scanning the entire HTML for existing links. It creates a snapshot of existing hrefs and checks against that instead.
        public async Task<string> InjectInternalLinksAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return content;

            var articles = await _cache.GetOrCreateAsync(
                "internal_link_articles_v2",
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes(10);

                    return await _articleService.GetRecentArticlesAsync(48);
                });

            if (articles == null || articles.Count == 0)
                return content;

            var baseUrl = _urlService.GetBaseUrl();

            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var textNodes = doc.DocumentNode
                .SelectNodes(
                    "//text()[normalize-space(.) != '' " +
                    "and not(ancestor::a) " +
                    "and not(ancestor::script) " +
                    "and not(ancestor::style) " +
                    "and not(ancestor::pre) " +
                    "and not(ancestor::code)]")
                ?.ToList();

            if (textNodes == null || textNodes.Count == 0)
                return content;

            const int maxLinks = 5;

            var linksAdded = 0;

            // Snapshot existing hrefs once.
            var existingLinks = doc.DocumentNode
                .SelectNodes("//a[@href]")
                ?.Select(a => a.GetAttributeValue("href", ""))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var article in articles)
            {
                if (linksAdded >= maxLinks)
                    break;

                if (article == null ||
                    string.IsNullOrWhiteSpace(article.Slug) ||
                    article.Category == null ||
                    string.IsNullOrWhiteSpace(article.Category.Slug))
                {
                    continue;
                }

                var url =
                    $"{baseUrl}/{article.Category.Slug}/{article.Slug}";

                // O(1) lookup instead of scanning the complete HTML.
                if (existingLinks.Contains(url))
                    continue;

                var keywords = (article.Title ?? string.Empty)
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => w.Trim(
                        '.', ',', '!', '?', ':', ';',
                        '"', '\'', '(', ')', '[', ']'))
                    .Where(w => w.Length > 4)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(2)
                    .ToList();

                foreach (var keyword in keywords)
                {
                    if (linksAdded >= maxLinks)
                        break;

                    var regex = new Regex(
                        Regex.Escape(keyword),
                        RegexOptions.IgnoreCase |
                        RegexOptions.CultureInvariant);

                    foreach (var node in textNodes)
                    {
                        if (linksAdded >= maxLinks)
                            break;

                        if (node.ParentNode == null)
                            continue;

                        var text = node.InnerText;

                        if (string.IsNullOrWhiteSpace(text))
                            continue;

                        var match = regex.Match(text);

                        if (!match.Success)
                            continue;

                        var matchedText =
                            text.Substring(
                                match.Index,
                                match.Length);

                        var safeUrl =
                            WebUtility.HtmlEncode(url);

                        var safeMatched =
                            WebUtility.HtmlEncode(matchedText);

                        var anchor =
                            doc.CreateElement("a");

                        anchor.SetAttributeValue(
                            "href",
                            safeUrl);

                        anchor.AppendChild(
                            doc.CreateTextNode(safeMatched));

                        var parent = node.ParentNode;

                        if (parent == null)
                            continue;

                        var before =
                            text.Substring(0, match.Index);

                        var after =
                            text.Substring(
                                match.Index + match.Length);

                        if (!string.IsNullOrEmpty(before))
                        {
                            parent.InsertBefore(
                                doc.CreateTextNode(before),
                                node);
                        }

                        parent.InsertBefore(anchor, node);

                        if (!string.IsNullOrEmpty(after))
                        {
                            parent.InsertBefore(
                                doc.CreateTextNode(after),
                                node);
                        }

                        parent.RemoveChild(node);

                        existingLinks.Add(url);

                        linksAdded++;

                        // Remove this node from the snapshot because
                        // it has already been replaced.
                        textNodes.Remove(node);

                        break;
                    }
                }
            }

            return doc.DocumentNode.InnerHtml;
        }

        //orignal method for injecting internal links, commented out for now. It fetches recent articles and injects links into the provided content based on keywords from article titles.
        //public async Task<string> InjectInternalLinksAsync(string content)
        //{
        //    if (string.IsNullOrWhiteSpace(content))
        //        return content;

        //    var articles = await _cache.GetOrCreateAsync("internal_link_articles_v1", async entry =>
        //    {
        //        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
        //        return await _articleService.GetRecentArticlesAsync(48);
        //    });

        //    var baseUrl = _urlService.GetBaseUrl();

        //    var doc = new HtmlDocument();
        //    doc.LoadHtml(content);

        //    // exclude script/style/pre/code and anchor descendants and only non-empty text
        //    var textNodes = doc.DocumentNode
        //        .SelectNodes("//text()[normalize-space(.) != '' and not(ancestor::a) and not(ancestor::script) and not(ancestor::style) and not(ancestor::pre) and not(ancestor::code)]");

        //    if (textNodes == null)
        //        return content;

        //    int linksAdded = 0;
        //    const int maxLinks = 5;

        //    // iterate articles and try to inject up to maxLinks anchors
        //    foreach (var article in articles)
        //    {
        //        if (linksAdded >= maxLinks) break;
        //        if (article == null || string.IsNullOrWhiteSpace(article.Slug) || article.Category == null || string.IsNullOrWhiteSpace(article.Category.Slug))
        //            continue;

        //        var url = $"{baseUrl}/{article.Category.Slug}/{article.Slug}";

        //        // avoid adding a link if the URL already exists in current DOM
        //        if (doc.DocumentNode.InnerHtml.Contains(url, StringComparison.OrdinalIgnoreCase))
        //            continue;

        //        var keywords = (article.Title ?? string.Empty)
        //            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        //            .Where(w => w.Length > 4)
        //            .Distinct(StringComparer.OrdinalIgnoreCase)
        //            .Take(2);

        //        foreach (var keyword in keywords)
        //        {
        //            if (linksAdded >= maxLinks) break;

        //            var pattern = Regex.Escape(keyword);
        //            // Use ToList() because we'll modify DOM while iterating
        //            foreach (var node in textNodes.ToList())
        //            {
        //                if (linksAdded >= maxLinks) break;
        //                if (node.ParentNode == null) continue;

        //                var text = node.InnerText ?? string.Empty;
        //                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        //                if (!match.Success) continue;

        //                // preserve original matched text's casing
        //                var matchedText = text.Substring(match.Index, match.Length);

        //                // HTML-encode components to avoid injecting unsafe content
        //                var safeUrl = WebUtility.HtmlEncode(url);
        //                var safeMatched = WebUtility.HtmlEncode(matchedText);
        //                var anchorHtml = $"<a href=\"{safeUrl}\">{safeMatched}</a>";

        //                // Build replacement fragment and insert parsed nodes in place of the text node
        //                var newHtml = ReplaceFirst(text, matchedText, anchorHtml);

        //                var fragDoc = new HtmlDocument();
        //                fragDoc.LoadHtml(newHtml);

        //                var parent = node.ParentNode;
        //                if (parent == null) continue;

        //                // Insert frag children before the existing text node, then remove the original text node
        //                foreach (var child in fragDoc.DocumentNode.ChildNodes.Reverse())
        //                {
        //                    // Insert in reverse order so final order matches newHtml
        //                    parent.InsertAfter(child, node);
        //                }
        //                parent.RemoveChild(node);

        //                linksAdded++;
        //                break; // move to next keyword/article
        //            }
        //        }
        //    }

        //    return doc.DocumentNode.InnerHtml;
        //}

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
