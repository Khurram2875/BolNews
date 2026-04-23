using System.Xml.Linq;
using BolNews.Application.Common;
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
        private readonly ICacheService _cache;


        public SitemapService(
            IArticleService articleService,
            ICategoryService categoryService,
            IUrlService urlService,
            ICacheService cache)
        {
            _articleService = articleService;
            _categoryService = categoryService;
            _urlService = urlService;
            _cache = cache;
        }

        // ✅ SITEMAP INDEX
        public async Task<string> GenerateSitemapIndexAsync()
        {
            return await _cache.GetOrCreateAsync(
                CacheKeys.Sitemap + "_index",
                async () =>
                {
                    var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");

                    var baseUrl = _urlService.GetBaseUrl();

                    var xml = new XDocument(
                        new XElement(ns + "sitemapindex",
                            new XElement(ns + "sitemap",
                                new XElement(ns + "loc", $"{baseUrl}/sitemap-articles.xml")
                            ),
                            new XElement(ns + "sitemap",
                                new XElement(ns + "loc", $"{baseUrl}/sitemap-categories.xml")
                            ),
                            new XElement(ns + "sitemap",
                                new XElement(ns + "loc", $"{baseUrl}/news-sitemap.xml")
                            )
                        )
                    );

                    return xml.ToString();
                },
                30
            );
        }

        // ✅ ARTICLES SITEMAP
        public async Task<string> GenerateArticleSitemapAsync()
        {
            return await _cache.GetOrCreateAsync(
                CacheKeys.Sitemap + "_articles",
                async () =>
                {
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

                    return new XDocument(new XElement(ns + "urlset", urls)).ToString();
                },
                10
            );
        }

        // ✅ CATEGORY SITEMAP
        public async Task<string> GenerateCategorySitemapAsync()
        {
            return await _cache.GetOrCreateAsync(
                CacheKeys.Sitemap + "_categories",
                async () =>
                {
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

                    return new XDocument(new XElement(ns + "urlset", urls)).ToString();
                },
                60
            );
        }
        public async Task<string> GenerateNewsSitemapAsync()
        {
            return await _cache.GetOrCreateAsync(
                CacheKeys.Sitemap + "_news",
                async () =>
                {
                    var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
                    var newsNs = XNamespace.Get("http://www.google.com/schemas/sitemap-news/0.9");

                    var baseUrl = _urlService.GetBaseUrl();

                    var fromDate = DateTime.UtcNow.AddHours(-48);
                    var articles = await _articleService.GetLatestPublishedAsync(fromDate, 1000);

                    var urls = articles.Select(a =>
                        new XElement(ns + "url",
                            new XElement(ns + "loc", $"{baseUrl}/news/{a.Category.Slug}/{a.Slug}"),
                            new XElement(newsNs + "news",
                                new XElement(newsNs + "publication",
                                    new XElement(newsNs + "name", "Bol News"),
                                    new XElement(newsNs + "language", "en")
                                ),
                                new XElement(newsNs + "publication_date",
                                    a.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                                new XElement(newsNs + "title", a.Title)
                            )
                        )
                    );

                    return new XDocument(
                        new XElement(ns + "urlset",
                            new XAttribute(XNamespace.Xmlns + "news", newsNs),
                            urls
                        )
                    ).ToString();
                },
                5
            );
        }
    }
}
