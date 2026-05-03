using System.Diagnostics;
using AutoMapper;
using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IArticleService _articleService;
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;

        public HomeController(ILogger<HomeController> logger, ICategoryService categoryService, IArticleService articleService, IMapper mapper, IMemoryCache cache)
        {
            _logger = logger;
            _categoryService = categoryService;
            _articleService = articleService;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<IActionResult> Index(string type = "today")
        {
            //var vm = new HomePageVM();
            var vm = await _cache.GetOrCreateAsync("homepage", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                var model = new HomePageVM();

                var topStory = await _articleService.GetTopStoryAsync();
                if (topStory != null)
                    model.TopStory = _mapper.Map<PublicArticleVM>(topStory);

                var secondary = await _articleService.GetSecondaryStoriesAsync();
                model.SecondaryStories = _mapper.Map<List<PublicArticleVM>>(secondary);

                var categories = await _categoryService.GetHomeCategoriesAsync();
                var categoryIds = categories.Select(c => c.Id).ToList();
                var articlesDict = await _articleService
               .GetArticlesForCategoriesAsync(categoryIds, 5);

                foreach (var category in categories)
                {
                    //var articles = await _articleService.GetArticlesByCategoryAsync(category.Id, 5);
                    articlesDict.TryGetValue(category.Id, out var articles);
                    model.CategorySections.Add(new CategorySectionVM
                    {
                        CategoryName = category.Name,
                        CategorySlug = category.Slug,
                        Articles = _mapper.Map<List<PublicArticleVM>>(articles)
                    });
                }
                return model;
            });
            ViewBag.Type = type;
            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public async Task<IActionResult> GetTrending(string type = "today")
        {
            var articles = await _articleService.GetTrendingAsync(5, type);
            var vm = _mapper.Map<List<PublicArticleVM>>(articles);

            return PartialView("~/Views/Shared/Components/TrendingNews/_TrendingList.cshtml", vm);
        }
    }
}
