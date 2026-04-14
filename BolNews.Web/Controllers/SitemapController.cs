using BolNews.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers
{
    public class SitemapController : Controller
    {
        private readonly IArticleService _articleService;

        public SitemapController(IArticleService articleService)
        {
            _articleService = articleService;
        }

        public async Task<IActionResult> Index()
        {
            var articles = await _articleService.GetAllAsync();

            var urls = articles.Select(a => $@"
<url>
    <loc>https://yourdomain.com/news/{a.Slug}</loc>
    <lastmod>{a.UpdatedAt:yyyy-MM-dd}</lastmod>
</url>");

            var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
{string.Join("", urls)}
</urlset>";

            return Content(xml, "application/xml");
        }
    }
}
