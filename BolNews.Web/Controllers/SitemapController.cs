using BolNews.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers
{
    public class SitemapController : Controller
    {
        private readonly ISitemapService _sitemapService;

        public SitemapController(ISitemapService sitemapService)
        {
            _sitemapService = sitemapService;
        }

        [HttpGet("/sitemap.xml")]
        public async Task<IActionResult> Index()
        {
            var xml = await _sitemapService.GenerateSitemapIndexAsync();
            return Content(xml, "application/xml; charset=utf-8");
        }

        [HttpGet("/sitemap-articles.xml")]
        public async Task<IActionResult> Articles()
        {
            var xml = await _sitemapService.GenerateArticleSitemapAsync();
            return Content(xml, "application/xml; charset=utf-8");
        }

        [HttpGet("/sitemap-categories.xml")]
        public async Task<IActionResult> Categories()
        {
            var xml = await _sitemapService.GenerateCategorySitemapAsync();
            return Content(xml, "application/xml; charset=utf-8");
        }

        [HttpGet("/news-sitemap.xml")]
        public async Task<IActionResult> NewsSitemap()
        {
            var xml = await _sitemapService.GenerateNewsSitemapAsync();
            return Content(xml, "application/xml; charset=utf-8");
        }

        [HttpGet("/sitemap-authors.xml")]
        public async Task<IActionResult> AuthorSitemap()
        {
            var xml = await _sitemapService.GenerateAuthorSitemapAsync();
            return Content(xml, "application/xml; charset=utf-8");
        }

        [HttpGet("/sitemap-tags.xml")]
        public async Task<IActionResult> TagSitemap()
        {
            var xml = await _sitemapService.GenerateTagSitemapAsync();
            return Content(xml, "application/xml; charset=utf-8");
        }
    }
}
