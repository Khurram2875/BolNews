using System.Xml.Linq;
using BolNews.Application.Interfaces;
using BolNews.Web.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Web.Services
{
    public class SitemapService : ISitemapService
    {
        private readonly IArticleService _articleService;
        private readonly ICategoryService _categoryService;
        private readonly IUrlService _urlService;
        private readonly IMemoryCache _cache;

        public SitemapService(
            IArticleService articleService,
            ICategoryService categoryService,
            IUrlService urlService,
            IMemoryCache cache)
        {
            _articleService = articleService;
            _categoryService = categoryService;
            _urlService = urlService;
            _cache = cache;
        }

        // ✅ SITEMAP INDEX
        public async Task<string> GenerateSitemapIndexAsync()
        {
            return await _cache.GetOrCreateAsync("sitemap_index", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

                var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");

                var baseUrl = _urlService.GetBaseUrl();

                var xml = new XDocument(
                    new XElement(ns + "sitemapindex",
                        new XElement(ns + "sitemap",
                            new XElement(ns + "loc", $"{baseUrl}/sitemap-articles.xml")
                        ),
                        new XElement(ns + "sitemap",
                            new XElement(ns + "loc", $"{baseUrl}/sitemap-categories.xml")
                        )
                    )
                );

                return xml.ToString();
            });
        }

        // ✅ ARTICLES SITEMAP
        public async Task<string> GenerateArticleSitemapAsync()
        {
            return await _cache.GetOrCreateAsync("sitemap_articles", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");

                var baseUrl = _urlService.GetBaseUrl();
                var articles = await _articleService.GetAllPublishedAsync();

                var urls = articles.Select(a =>
                    new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}/news/{a.Category.Slug}/{a.Slug}"),
                        new XElement(ns + "lastmod",
                            (a.UpdatedAt ?? a.PublishedAt)?.ToString("yyyy-MM-dd")),
                        new XElement(ns + "changefreq", "hourly"),
                        new XElement(ns + "priority", "0.9")
                    )
                );

                var xml = new XDocument(
                    new XElement(ns + "urlset", urls)
                );

                return xml.ToString();
            });
        }

        // ✅ CATEGORY SITEMAP
        public async Task<string> GenerateCategorySitemapAsync()
        {
            return await _cache.GetOrCreateAsync("sitemap_categories", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);

                var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");

                var baseUrl = _urlService.GetBaseUrl();
                var categories = await _categoryService.GetAllAsync();

                var urls = categories.Select(c =>
                    new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}/news/{c.Slug}"),
                        new XElement(ns + "changefreq", "daily"),
                        new XElement(ns + "priority", "0.8")
                    )
                );

                var xml = new XDocument(
                    new XElement(ns + "urlset", urls)
                );

                return xml.ToString();
            });
        }
        public async Task<string> GenerateNewsSitemapAsync()
        {
            return await _cache.GetOrCreateAsync("sitemap_news", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
                var newsNs = XNamespace.Get("http://www.google.com/schemas/sitemap-news/0.9");

                var baseUrl = _urlService.GetBaseUrl();

                // 🔥 ONLY last 48 hours
                var fromDate = DateTime.UtcNow.AddHours(-48);

                var articles = await _articleService.GetLatestPublishedAsync(fromDate, 1000);
                // 👉 create this method

                var urls = articles.Select(a =>
                    new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}/news/{a.Category.Slug}/{a.Slug}"),

                        new XElement(newsNs + "news",
                            new XElement(newsNs + "publication",
                                new XElement(newsNs + "name", "Bol News"),
                                new XElement(newsNs + "language", "en") // or "ur"
                            ),

                            new XElement(newsNs + "publication_date",
                                a.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ")),

                            new XElement(newsNs + "title", a.Title)
                        )
                    )
                );

                var xml = new XDocument(
                    new XElement(ns + "urlset",
                        new XAttribute(XNamespace.Xmlns + "news", newsNs),
                        urls
                    )
                );

                return xml.ToString();
            });
        }
    }
}
