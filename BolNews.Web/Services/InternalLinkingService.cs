using BolNews.Application.Interfaces;
using BolNews.Web.Interfaces;

namespace BolNews.Web.Services
{
    public class InternalLinkingService : IInternalLinkingService
    {
        private readonly IArticleService _articleService;
        private readonly IUrlService _urlService;

        public InternalLinkingService(
            IArticleService articleService,
            IUrlService urlService)
        {
            _articleService = articleService;
            _urlService = urlService;
        }

        public async Task<string> InjectInternalLinksAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return content;

            var articles = await _articleService.GetRecentArticlesAsync(48);
            var baseUrl = _urlService.GetBaseUrl();

            foreach (var article in articles.Take(5))
            {
                if (content.Contains(article.Slug))
                    continue;

                if (content.Contains("<a"))
                    continue;

                var keywords = article.Title
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 4)
                    .Take(2);

                foreach (var keyword in keywords)
                {
                    if (!content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var url = $"{baseUrl}/news/{article.Category.Slug}/{article.Slug}";

                    content = ReplaceFirst(
                        content,
                        keyword,
                        $"<a href=\"{url}\">{keyword}</a>"
                    );

                    break;
                }
            }

            return content;
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
