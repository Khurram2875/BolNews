using BolNews.Application.Interfaces;
using BolNews.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Web.Controllers
{
    public class SitemapController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly ICategoryService _categoryService;
        private readonly ISitemapService _sitemapService;
        

        public SitemapController(IArticleService articleService, ICategoryService categoryService, ISitemapService sitemapService)
        {
            _articleService = articleService;
            _categoryService = categoryService;
            _sitemapService = sitemapService;
            
        }
        [HttpGet("/sitemap.xml")]
        public async Task<IActionResult> Index()
        {
            var xml = await _sitemapService.GenerateSitemapIndexAsync();
            return Content(xml, "application/xml");
        }

        [HttpGet("/sitemap-articles.xml")]
        public async Task<IActionResult> Articles()
        {
            var xml = await _sitemapService.GenerateArticleSitemapAsync();
            return Content(xml, "application/xml");
        }

        [HttpGet("/sitemap-categories.xml")]
        public async Task<IActionResult> Categories()
        {
            var xml = await _sitemapService.GenerateCategorySitemapAsync();
            return Content(xml, "application/xml");
        }
        [HttpGet("/news-sitemap.xml")]
        public async Task<IActionResult> NewsSitemap()
        {
            var xml = await _sitemapService.GenerateNewsSitemapAsync();
            return Content(xml, "application/xml");
        }
        [HttpGet("sitemap-authors.xml")]
        public async Task<IActionResult> AuthorSitemap()
        {
            var xml = await _sitemapService.GenerateAuthorSitemapAsync();
            return Content(xml, "application/xml");
        }
        //[HttpGet("/sitemap.xml")]
        //public async Task<IActionResult> Index()
        //{
        //    var articles = await _articleService.GetAllPublishedAsync();
        //    var categories = await _categoryService.GetAllAsync();

        //    var urls = new List<string>();

        //    string baseUrl = $"{Request.Scheme}://{Request.Host}";

        //    // Articles
        //    foreach (var article in articles)
        //    {
        //        urls.Add($@"
        //                <url>
        //                    <loc>{baseUrl}/news/{article.Category?.Slug}/{article.Slug}</loc>
        //                    <lastmod>{article.UpdatedAt?.ToString("yyyy-MM-dd")}</lastmod>
        //                     <changefreq>daily</changefreq>
        //                        <priority>0.8</priority>
        //                </url>");
        //    }

        //    // Categories
        //    foreach (var category in categories)
        //    {
        //        urls.Add($@"
        //                <url>
        //                    <loc>{baseUrl}/news/{category.Slug}</loc>
        //                    <changefreq>daily</changefreq>
        //                        <priority>0.6</priority>
        //                </url>");
        //    }

        //    var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
        //            <urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
        //                {string.Join("", urls)}
        //            </urlset>";

        //    return Content(xml, "application/xml");
        //}
    }
}
