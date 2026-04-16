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
            if (string.IsNullOrWhiteSpace(q))
                return RedirectToAction("Index", "Home");

            int pageSize = 10;

            var articles = await _articleService.SearchAsync(q, page, pageSize);

            var vm = _mapper.Map<List<PublicArticleVM>>(articles);

            ViewBag.Query = q;
            ViewBag.CurrentPage = page;
            ViewBag.HasNextPage = articles.Count == pageSize;

            // SEO
            ViewBag.MetaTitle = $"Search results for '{q}'";
            ViewBag.MetaDescription = $"Search results for {q} on Bol News";

            return View(vm);
        }
    }
}
