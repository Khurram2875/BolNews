using System.Diagnostics;
using AutoMapper;
using BolNews.Application.Common;
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
        private readonly IEditorialPlacementService _editorialPlacementService;
        private readonly IMemoryCache _cache;
        // Add this field to the HomeController class
        private static CancellationTokenSource ResetToken = new CancellationTokenSource();
        public HomeController(ILogger<HomeController> logger, ICategoryService categoryService, IArticleService articleService, IMapper mapper, IMemoryCache cache, IEditorialPlacementService editorialPlacementService)
        {
            _logger = logger;
            _categoryService = categoryService;
            _articleService = articleService;
            _mapper = mapper;
            _cache = cache;
            _editorialPlacementService = editorialPlacementService;
        }

        public async Task<IActionResult> Index(string type = "today")
        {
            //ClearLatestNewsCache(5);
             //var vm = new HomePageVM();
             var vm = await _cache.GetOrCreateAsync(CacheKeys.HomePageIndex, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                var model = new HomePageVM();

                var topStory = await _articleService.GetTopStoryAsync();
               
                if (topStory != null)
                    model.TopStory = _mapper.Map<PublicArticleVM>(topStory);

                var secondary = await _articleService.GetSecondaryStoriesAsync(50);
                model.SecondaryStories = _mapper.Map<List<PublicArticleVM>>(secondary);
                model.PinnedSecondaryStoryCount = (await _editorialPlacementService.GetPinnedSecondaryStoriesAsync()).Count;

                 // Use GetParentCategoriesWithChildrenAsync so we know which
                // parent categories have subcategories (e.g. Sports → Cricket, Football)
                var categories = await _categoryService.GetParentCategoriesWithChildrenAsync();

                // Build a flat map: parentCategoryId → [parentId, subId1, subId2, ...]
                // This lets us fetch articles from sub-categories and display them
                // under the parent section (Sports shows Cricket + Football articles)
                var categoryIdMap = categories.ToDictionary(
                    c => c.Id,
                    c =>
                    {
                        var ids = new List<int> { c.Id };
                        if (c.SubCategories != null)
                            ids.AddRange(c.SubCategories.Select(s => s.Id));
                        return ids;
                    }
                );

                // Fetch articles for ALL relevant IDs in one DB call
                var allCategoryIds = categoryIdMap.Values.SelectMany(ids => ids).Distinct().ToList();
                var articlesDict = await _articleService.GetArticlesForCategoriesAsync(allCategoryIds, 5);

                var displayOrder = new[]
                    {
                        "pakistan",
                        "world-news",
                        "business",
                        "sports",
                        "entertainment",
                        "technology",
                        "health",
                        "lifestyle"
                       

                    };

                var orderedCategories = displayOrder
                    .Select(slug => categories.FirstOrDefault(c =>
                        string.Equals(c.Slug, slug, StringComparison.OrdinalIgnoreCase)))
                    .Where(c => c != null)
                    .ToList();

                foreach (var category in orderedCategories)
                {
                    // Merge articles from the parent + all its subcategories
                    var relevantIds = categoryIdMap[category.Id];
                    var mergedArticles = relevantIds
                        .Where(id => articlesDict.ContainsKey(id))
                        .SelectMany(id => articlesDict[id])
                        .OrderByDescending(a => a.PublishedAt)
                        .Take(5)
                        .ToList();

                    // Skip categories that have no articles at all
                    // (neither direct nor via subcategories)
                    if (!mergedArticles.Any())
                        continue;

                    model.CategorySections.Add(new CategorySectionVM
                    {
                        CategoryName = category.Name,
                        CategorySlug = category.Slug,
                        Articles = _mapper.Map<List<PublicArticleVM>>(mergedArticles)
                    });
                }

                return model;
            });
            ViewBag.Type = type;
            return View(vm);
        }
        public async Task<IActionResult> Index1(string type = "today")
        {
            //ClearLatestNewsCache(5);
            //var vm = new HomePageVM();
            var vm = await _cache.GetOrCreateAsync(CacheKeys.HomePageIndex1, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                var model = new HomePageVM();

                var topStory = await _articleService.GetTopStoryAsync();

                if (topStory != null)
                    model.TopStory = _mapper.Map<PublicArticleVM>(topStory);

                var secondary = await _articleService.GetSecondaryStoriesAsync(50);
                model.SecondaryStories = _mapper.Map<List<PublicArticleVM>>(secondary);
                model.PinnedSecondaryStoryCount = (await _editorialPlacementService.GetPinnedSecondaryStoriesAsync()).Count;

                // Use GetParentCategoriesWithChildrenAsync so we know which
                // parent categories have subcategories (e.g. Sports → Cricket, Football)
                var categories = await _categoryService.GetParentCategoriesWithChildrenAsync();

                // Build a flat map: parentCategoryId → [parentId, subId1, subId2, ...]
                // This lets us fetch articles from sub-categories and display them
                // under the parent section (Sports shows Cricket + Football articles)
                var categoryIdMap = categories.ToDictionary(
                    c => c.Id,
                    c =>
                    {
                        var ids = new List<int> { c.Id };
                        if (c.SubCategories != null)
                            ids.AddRange(c.SubCategories.Select(s => s.Id));
                        return ids;
                    }
                );

                // Fetch articles for ALL relevant IDs in one DB call
                var allCategoryIds = categoryIdMap.Values.SelectMany(ids => ids).Distinct().ToList();
                var articlesDict = await _articleService.GetArticlesForCategoriesAsync(allCategoryIds, 4);

                var displayOrder = new[]
                {
                    "pakistan",
                    "world-news",
                    "business",
                    "sports",
                    "entertainment",
                    "technology",
                    "health",
                    "lifestyle"


                };

                var orderedCategories = displayOrder
                    .Select(slug => categories.FirstOrDefault(c =>
                        string.Equals(c.Slug, slug, StringComparison.OrdinalIgnoreCase)))
                    .Where(c => c != null)
                    .ToList();

                foreach (var category in orderedCategories)
                {
                    // Merge articles from the parent + all its subcategories
                    var relevantIds = categoryIdMap[category.Id];
                    var mergedArticles = relevantIds
                        .Where(id => articlesDict.ContainsKey(id))
                        .SelectMany(id => articlesDict[id])
                        .OrderByDescending(a => a.PublishedAt)
                        .Take(5)
                        .ToList();

                    // Skip categories that have no articles at all
                    // (neither direct nor via subcategories)
                    if (!mergedArticles.Any())
                        continue;



                    model.CategorySections.Add(new CategorySectionVM
                    {
                        CategoryName = category.Name,
                        CategorySlug = category.Slug,
                        Articles = _mapper.Map<List<PublicArticleVM>>(mergedArticles)
                    });
                }

                return model;
            });
            ViewBag.Type = type;
            return View(vm);
        }
        [HttpGet("/privacy-policy")]
        public IActionResult Privacy() => Information("privacy");

        [HttpGet("/contact-us")]
        public IActionResult ContactUs() => Information("contact");

        [HttpGet("/advertise")]
        public IActionResult Advertise() => Information("advertise");

        [HttpGet("/blogs")]
        public IActionResult Blogs() => Information("blogs");

        [HttpGet("/branded-content")]
        public IActionResult BrandedContent() => Information("branded-content");

        [HttpGet("/editorial-policy")]
        public IActionResult EditorialPolicy() => Information("editorial-policy");

        [HttpGet("/terms-of-service")]
        public IActionResult TermsOfService() => Information("terms-of-service");
        public IActionResult Live()
        {
            return View();
        }
        public void ClearAllCache()
        {
            // Signal the token to cancel, evicting all bound entries
            ResetToken.Cancel();

            // Re-initialize the token so future cache entries can use it
            ResetToken = new CancellationTokenSource();
        }
        public void ClearLatestNewsCache(int count = 5)
        {
            var home = CacheKeys.HomePage;
            var cacheKey = $"latest_news_{count}";
            _cache.Remove(home);
            _cache.Remove(cacheKey);
            // The next time InvokeAsync is called, it will be forced to fetch fresh data.
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
        [HttpGet("/about-us")]
        public IActionResult About() => Information("about");

        private IActionResult Information(string page)
        {
            ViewData["InformationPage"] = page;
            return View("Information");
        }
    }
}
