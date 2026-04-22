using BolNews.Application.Interfaces;
using BolNews.Application.Services;
using BolNews.Web.Interfaces;
using HtmlAgilityPack;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Web.Services
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

            var articles = await _cache.GetOrCreateAsync("internal_link_articles", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await _articleService.GetRecentArticlesAsync(48);
            });
            var baseUrl = _urlService.GetBaseUrl();

        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        var textNodes = doc.DocumentNode
            .SelectNodes("//text()[not(ancestor::a)]"); // 🚀 avoid existing links

        if (textNodes == null)
            return content;

        int linksAdded = 0;
        int maxLinks = 5;

        foreach (var article in articles)
        {
            if (linksAdded >= maxLinks)
                break;

                // ❌ Prevent self-linking
                var url = $"{baseUrl}/news/{article.Category.Slug}/{article.Slug}";
                if (content.Contains(url))
                continue;

            var keywords = article.Title
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 4)
                .Take(2);

            foreach (var keyword in keywords)
            {
                foreach (var node in textNodes)
                {
                    if (linksAdded >= maxLinks)
                        break;

                    var text = node.InnerText;

                    if (!text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        continue;

                    //var url = $"{baseUrl}/news/{article.Category.Slug}/{article.Slug}";

                    // 🔥 Replace only first occurrence in THIS node
                    var newHtml = ReplaceFirst(
                        text,
                        keyword,
                        $"<a href=\"{url}\">{keyword}</a>"
                    );

                    var newNode = HtmlNode.CreateNode($"<span>{newHtml}</span>");

                    node.ParentNode.ReplaceChild(newNode, node);

                    linksAdded++;
                    break;
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
