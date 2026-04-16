using BolNews.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers
{
    public class SitemapController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly ICategoryService _categoryService;

        public SitemapController(IArticleService articleService, ICategoryService categoryService)
        {
            _articleService = articleService;
            _categoryService = categoryService;
        }

        [HttpGet("/sitemap.xml")]
        public async Task<IActionResult> Index()
        {
            var articles = await _articleService.GetAllPublishedAsync();
            var categories = await _categoryService.GetAllAsync();

            var urls = new List<string>();

            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            // Articles
            foreach (var article in articles)
            {
                urls.Add($@"
                        <url>
                            <loc>{baseUrl}/news/{article.Category?.Slug}/{article.Slug}</loc>
                            <lastmod>{article.UpdatedAt?.ToString("yyyy-MM-dd")}</lastmod>
                             <changefreq>daily</changefreq>
                                <priority>0.8</priority>
                        </url>");
            }

            // Categories
            foreach (var category in categories)
            {
                urls.Add($@"
                        <url>
                            <loc>{baseUrl}/news/{category.Slug}</loc>
                            <changefreq>daily</changefreq>
                                <priority>0.6</priority>
                        </url>");
            }

            var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                    <urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
                        {string.Join("", urls)}
                    </urlset>";

            return Content(xml, "application/xml");
        }
    }
}
