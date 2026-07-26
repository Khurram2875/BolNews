using AutoMapper;
using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers
{
    public class SearchController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly IMapper _mapper;

        public SearchController(IArticleService articleService, IMapper mapper)
        {
            _articleService = articleService;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index(string q, int page = 1)
        {
            ViewBag.Query = q ?? "";
            ViewBag.CurrentPage = page;

            if (string.IsNullOrWhiteSpace(q))
            {
                ViewBag.MetaTitle = "Search";
                ViewBag.MetaDescription = "Search Bol News for the latest stories.";
                ViewBag.HasNextPage = false;
                return View(new List<PublicArticleVM>());   // empty result set, shows the search bar with no query yet
            }

            int pageSize = 10;

            var articles = await _articleService.SearchAsync(q, page, pageSize);

            var vm = _mapper.Map<List<PublicArticleVM>>(articles);

            ViewBag.HasNextPage = articles.Count == pageSize;

            ViewBag.MetaTitle = $"Search results for '{q}'";
            ViewBag.MetaDescription = $"Search results for {q} on Bol News";

            return View(vm);
        }
    }
}
