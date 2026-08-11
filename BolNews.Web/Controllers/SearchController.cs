using AutoMapper;
using BolNews.Application.Common;
using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers
{
    public class SearchController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public SearchController(IArticleService articleService, IMapper mapper, ICacheService cacheService)
        {
            _articleService = articleService;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<IActionResult> Index(string q, int page = 1)
        {
            if (page < 1)
            {
                page = 1;
            }

            if (string.IsNullOrWhiteSpace(q))
            {
                ViewBag.Query = "";
                ViewBag.CurrentPage = page;
                ViewBag.MetaTitle = "Search";
                ViewBag.MetaDescription = "Search Bol News for the latest stories.";
                ViewBag.HasNextPage = false;
                return View(new List<PublicArticleVM>());   // empty result set, shows the search bar with no query yet
            }

            const int pageSize = 10;

            var normalizedQuery = q.Trim();
            var cacheKey = CacheKeys.Search(normalizedQuery, page);

            var articles = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () => await _articleService.SearchAsync(normalizedQuery, page, pageSize + 1),
                2
            );

            var vm = _mapper.Map<List<PublicArticleVM>>(articles.Take(pageSize));

            ViewBag.HasNextPage = articles.Count > pageSize;
            ViewBag.Query = normalizedQuery;
            ViewBag.CurrentPage = page;
            ViewBag.MetaTitle = $"Search results for '{normalizedQuery}'";
            ViewBag.MetaDescription = $"Search results for {normalizedQuery} on Bol News";

            return View(vm);
        }
    }
}
