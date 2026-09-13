using AutoMapper;
using BolNews.Application.Common;
using BolNews.Application.Interfaces;
using BolNews.Domain.Common;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace BolNews.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IArticleService _articleService;
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;
        private readonly IEditorialPlacementService _editorialPlacementService;
        private static readonly SemaphoreSlim _homeCacheLock = new(1, 1);
        private readonly IMemoryCache _cache;
        private readonly IEditorialCategoryConfigurationService _editorialCategoryConfigurationService;
        private static CancellationTokenSource ResetToken = new CancellationTokenSource();
        public HomeController(ILogger<HomeController> logger, ICategoryService categoryService, IArticleService articleService, IMapper mapper, IMemoryCache cache, IEditorialPlacementService editorialPlacementService, IEditorialCategoryConfigurationService editorialCategoryConfigurationService)
        {
            _logger = logger;
            _categoryService = categoryService;
            _articleService = articleService;
            _mapper = mapper;
            _cache = cache;
            _editorialPlacementService = editorialPlacementService;
            _editorialCategoryConfigurationService = editorialCategoryConfigurationService;
        }

        public async Task<IActionResult> Index(string type = "today")
        {
            // WordPress historically exposed published posts as /?p=123.
            // Resolve only known published WordPress imports and permanently
            // redirect them to the current canonical article URL. Unknown
            // numeric IDs return 404 instead of becoming a homepage soft-404.
            var legacyPostId = Request.Query["p"].FirstOrDefault();
            if (int.TryParse(legacyPostId, out var postId) && postId > 0)
            {
                var article = await _articleService.GetPublishedBySourceAsync(
                    "WordPress",
                    postId.ToString());

                if (article == null)
                    return NotFound();

                return RedirectToRoutePermanent("articleDetails", new
                {
                    categorySlug = article.Category.Slug,
                    slug = article.Slug
                });
            }

            var cacheKey = CacheKeys.HomePage + "_Index";

            if (!_cache.TryGetValue(cacheKey, out HomePageVM vm))
            {
                await _homeCacheLock.WaitAsync();
                try
                {
                    if (!_cache.TryGetValue(cacheKey, out vm))
                    {
                        vm = await BuildHomePageAsync();
                        _cache.Set(cacheKey, vm, TimeSpan.FromMinutes(5));
                    }
                }
                finally
                {
                    _homeCacheLock.Release();
                }
            }

            ViewBag.Type = type;
            ViewBag.MetaTitle = "Bol News – Pakistan Breaking News, Politics, Business, Sports & Entertainment";
            ViewBag.MetaDescription = "Bol News delivers breaking news, in-depth analysis and live coverage from Pakistan and around the world — politics, business, sports, entertainment and technology.";
            return View(vm);
        }

        private async Task<HomePageVM> BuildHomePageAsync()
        {
            var model = new HomePageVM();

            var categoryConfigurations =
                await _editorialCategoryConfigurationService
                    .GetAllAsync();

            List<int> GetConfiguredCategoryIds(string placementType)
            {
                return categoryConfigurations.TryGetValue(
                    placementType,
                    out var configurations)
                    ? configurations
                        .Where(x => x.IsActive)
                        .OrderBy(x => x.SortOrder)
                        .Select(x => x.CategoryId)
                        .Distinct()
                        .ToList()
                    : new List<int>();
            }

            var pinnedTop =
                await _editorialPlacementService
                    .GetPinnedTopStoryAsync();

            PublicArticleVM? topStory;

            if (pinnedTop != null)
            {
                topStory =
                    _mapper.Map<PublicArticleVM>(
                        pinnedTop.Article);
            }
            else
            {
                var topStoryCategoryIds =
                    GetConfiguredCategoryIds(
                        EditorialPlacementKeys.HomepageTopStory);

                if (topStoryCategoryIds.Count > 0)
                {
                    var configuredTopStories =
                        await _articleService
                            .GetLatestArticlesForCategoriesAsync(
                                topStoryCategoryIds,
                                1);

                    if (configuredTopStories.Count > 0)
                    {
                        topStory =
                            _mapper.Map<PublicArticleVM>(
                                configuredTopStories[0]);
                    }
                    else
                    {
                        topStory =
                            _mapper.Map<PublicArticleVM>(
                                await _articleService
                                    .GetTopStoryAsync());
                    }
                }
                else
                {
                    topStory =
                        _mapper.Map<PublicArticleVM>(
                            await _articleService
                                .GetTopStoryAsync());
                }
            }

            model.TopStory = topStory;

            var secondaryCategoryIds =
                GetConfiguredCategoryIds(
                    EditorialPlacementKeys.HomepageSecondaryStory);

            List<PublicArticleVM> secondaryAll;

            if (secondaryCategoryIds.Count > 0)
            {
                var configuredSecondary =
                    await _articleService
                        .GetLatestArticlesForCategoriesAsync(
                            secondaryCategoryIds,
                            50);

                secondaryAll =
                    _mapper.Map<List<PublicArticleVM>>(
                        configuredSecondary);
            }
            else
            {
                var secondaryRaw =
                    await _articleService
                        .GetSecondaryStoriesAsync(50);

                secondaryAll =
                    _mapper.Map<List<PublicArticleVM>>(
                        secondaryRaw);
            }

            var pinnedSecondaryPlacements =
                await _editorialPlacementService
                    .GetPinnedSecondaryStoriesAsync();

            var pinnedLatestPlacements =
                await _editorialPlacementService
                    .GetPinnedLatestStoriesAsync();

            var pinnedFeaturedPlacements =
                await _editorialPlacementService
                    .GetPinnedFeaturedStoriesAsync();

            var pinnedSecondary =
                pinnedSecondaryPlacements
                    .OrderBy(p => p.SortOrder)
                    .Select(p =>
                        _mapper.Map<PublicArticleVM>(
                            p.Article))
                    .ToList();

            var pinnedLatest =
                pinnedLatestPlacements
                    .OrderBy(p => p.SortOrder)
                    .Select(p =>
                        _mapper.Map<PublicArticleVM>(
                            p.Article))
                    .ToList();

            var pinnedFeatured =
                pinnedFeaturedPlacements
                    .OrderBy(p => p.SortOrder)
                    .Select(p =>
                        _mapper.Map<PublicArticleVM>(
                            p.Article))
                    .ToList();

            var pinnedIds =
                new HashSet<int>(
                    pinnedSecondary
                        .Select(a => a.Id)
                        .Concat(
                            pinnedLatest.Select(a => a.Id))
                        .Concat(
                            pinnedFeatured.Select(a => a.Id)));

            if (topStory != null)
            {
                pinnedIds.Add(topStory.Id);
            }

            var organicSecondary =
                secondaryAll
                    .Where(a => !pinnedIds.Contains(a.Id))
                    .ToList();

            model.SecondaryStories =
                pinnedSecondary
                    .Concat(organicSecondary)
                    .ToList();

            model.PinnedSecondaryStoryCount =
                pinnedSecondary.Count;

            model.PinnedLatestStories =
                pinnedLatest;

            model.PinnedFeaturedStories =
                pinnedFeatured;

            var featuredCategoryIds =
                GetConfiguredCategoryIds(
                    EditorialPlacementKeys.HomepageFeaturedStory);

            List<PublicArticleVM> configuredFeaturedPool;

            if (featuredCategoryIds.Count > 0)
            {
                var configuredFeatured =
                    await _articleService
                        .GetLatestArticlesForCategoriesAsync(
                            featuredCategoryIds,
                            50);

                configuredFeaturedPool =
                    _mapper.Map<List<PublicArticleVM>>(
                        configuredFeatured);
            }
            else
            {
                configuredFeaturedPool =
                    secondaryAll;
            }

            var organicFeatured =
                configuredFeaturedPool
                    .Where(a => !pinnedIds.Contains(a.Id))
                    .ToList();

            model.FeaturedStories =
                organicFeatured;

            var categories =
                await _categoryService
                    .GetParentCategoriesWithChildrenAsync();

            var categoryIdMap =
                categories.ToDictionary(
                    c => c.Id,
                    c =>
                    {
                        var ids =
                            new List<int>
                            {
                                c.Id
                            };

                        if (c.SubCategories != null)
                        {
                            ids.AddRange(
                                c.SubCategories.Select(
                                    s => s.Id));
                        }

                        return ids;
                    });

            var allCategoryIds =
                categoryIdMap.Values
                    .SelectMany(ids => ids)
                    .Distinct()
                    .ToList();

            var articlesDict =
                await _articleService
                    .GetArticlesForCategoriesAsync(
                        allCategoryIds,
                        5);

            var displayOrder = new[]
            {
                "pakistan",
                "world",
                "business",
                "sports",
                "entertainment",
                "technology",
                "health",
                "lifestyle"
            };

            var orderedCategories =
                displayOrder
                    .Select(slug =>
                        categories.FirstOrDefault(c =>
                            string.Equals(
                                c.Slug,
                                slug,
                                StringComparison.OrdinalIgnoreCase)))
                    .Where(c => c != null)
                    .ToList();

            foreach (var category in orderedCategories)
            {
                var relevantIds =
                    categoryIdMap[category.Id];

                var mergedArticles =
                    relevantIds
                        .Where(id =>
                            articlesDict.ContainsKey(id))
                        .SelectMany(id =>
                            articlesDict[id])
                        .OrderByDescending(a =>
                            a.PublishedAt)
                        .Take(5)
                        .ToList();

                if (!mergedArticles.Any())
                {
                    continue;
                }

                model.CategorySections.Add(
                    new CategorySectionVM
                    {
                        CategoryName =
                            category.Name,

                        CategorySlug =
                            category.Slug,

                        Articles =
                            _mapper.Map<List<PublicArticleVM>>(
                                mergedArticles)
                    });
            }

            return model;
        }

        public async Task<IActionResult> Index1(string type = "today")
        {
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

                var categories = await _categoryService.GetParentCategoriesWithChildrenAsync();

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

                var allCategoryIds = categoryIdMap.Values.SelectMany(ids => ids).Distinct().ToList();
                var articlesDict = await _articleService.GetArticlesForCategoriesAsync(allCategoryIds, 4);

                var displayOrder = new[]
                {
                    "pakistan",
                    "world",
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
                    var relevantIds = categoryIdMap[category.Id];
                    var mergedArticles = relevantIds
                        .Where(id => articlesDict.ContainsKey(id))
                        .SelectMany(id => articlesDict[id])
                        .OrderByDescending(a => a.PublishedAt)
                        .Take(5)
                        .ToList();

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
            ResetToken.Cancel();
            ResetToken = new CancellationTokenSource();
        }
        public void ClearLatestNewsCache(int count = 5)
        {
            var home = CacheKeys.HomePage;
            var cacheKey = $"latest_news_{count}";
            _cache.Remove(home);
            _cache.Remove(cacheKey);
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
        [Route("/error/{code:int}")]
        public IActionResult StatusCodeError(int code)
        {
            Response.StatusCode = code;

            var vm = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = code
            };

            return code switch
            {
                404 => View("NotFound", vm),
                403 => View("Forbidden", vm),
                _ => View("Error", vm)
            };
        }
    }
}