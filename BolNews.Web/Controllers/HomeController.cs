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
        // Add this field to the HomeController class
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
        /// <summary>
        /// /old index method, kept for reference. The new Index1 method is used for the actual homepage rendering.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        //public async Task<IActionResult> Index(string type = "today")
        //{
        //    var vm = await _cache.GetOrCreateAsync(CacheKeys.HomePage + "_Index", async entry =>
        //    {
        //        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

        //        var model = new HomePageVM();

        //        // ── Top story: editorial pin overrides the organic pick ────────
        //        var pinnedTop = await _editorialPlacementService.GetPinnedTopStoryAsync();
        //        var topStory = pinnedTop != null
        //            ? _mapper.Map<PublicArticleVM>(pinnedTop.Article)
        //            : _mapper.Map<PublicArticleVM>(await _articleService.GetTopStoryAsync());

        //        model.TopStory = topStory;

        //        // ── Raw chronological pool ──────────────────────────────────────
        //        var secondaryRaw = await _articleService.GetSecondaryStoriesAsync(50);
        //        var secondaryAll = _mapper.Map<List<PublicArticleVM>>(secondaryRaw);

        //        // ── Independent pinned lists for Secondary / Latest / Featured ──
        //        var pinnedSecondaryPlacements = await _editorialPlacementService.GetPinnedSecondaryStoriesAsync();
        //        var pinnedLatestPlacements = await _editorialPlacementService.GetPinnedLatestStoriesAsync();
        //        var pinnedFeaturedPlacements = await _editorialPlacementService.GetPinnedFeaturedStoriesAsync();

        //        var pinnedSecondary = pinnedSecondaryPlacements.OrderBy(p => p.SortOrder)
        //            .Select(p => _mapper.Map<PublicArticleVM>(p.Article)).ToList();
        //        var pinnedLatest = pinnedLatestPlacements.OrderBy(p => p.SortOrder)
        //            .Select(p => _mapper.Map<PublicArticleVM>(p.Article)).ToList();
        //        var pinnedFeatured = pinnedFeaturedPlacements.OrderBy(p => p.SortOrder)
        //            .Select(p => _mapper.Map<PublicArticleVM>(p.Article)).ToList();

        //        // ── Strip anything already pinned anywhere (+ the top story) from the organic pool ──
        //        var pinnedIds = new HashSet<int>(
        //            pinnedSecondary.Select(a => a.Id)
        //                .Concat(pinnedLatest.Select(a => a.Id))
        //                .Concat(pinnedFeatured.Select(a => a.Id)));
        //        if (topStory != null) pinnedIds.Add(topStory.Id);

        //        var organicPool = secondaryAll.Where(a => !pinnedIds.Contains(a.Id)).ToList();

        //        // ── Secondary: pinned first, organic fallback fills the rest ────
        //        model.SecondaryStories = pinnedSecondary.Concat(organicPool).ToList();
        //        model.PinnedSecondaryStoryCount = pinnedSecondary.Count;

        //        // ── Latest / Featured: pinned lists passed through as-is ────────
        //        model.PinnedLatestStories = pinnedLatest;
        //        model.PinnedFeaturedStories = pinnedFeatured;

        //        // ── Category sections (unchanged from before) ───────────────────
        //        var categories = await _categoryService.GetParentCategoriesWithChildrenAsync();

        //        var categoryIdMap = categories.ToDictionary(
        //            c => c.Id,
        //            c =>
        //            {
        //                var ids = new List<int> { c.Id };
        //                if (c.SubCategories != null)
        //                    ids.AddRange(c.SubCategories.Select(s => s.Id));
        //                return ids;
        //            }
        //        );

        //        var allCategoryIds = categoryIdMap.Values.SelectMany(ids => ids).Distinct().ToList();
        //        var articlesDict = await _articleService.GetArticlesForCategoriesAsync(allCategoryIds, 5);

        //        var displayOrder = new[]
        //        {
        //    "pakistan", "world-news", "business", "sports",
        //    "entertainment", "technology", "health", "lifestyle"
        //};

        //        var orderedCategories = displayOrder
        //            .Select(slug => categories.FirstOrDefault(c =>
        //                string.Equals(c.Slug, slug, StringComparison.OrdinalIgnoreCase)))
        //            .Where(c => c != null)
        //            .ToList();

        //        foreach (var category in orderedCategories)
        //        {
        //            var relevantIds = categoryIdMap[category.Id];
        //            var mergedArticles = relevantIds
        //                .Where(id => articlesDict.ContainsKey(id))
        //                .SelectMany(id => articlesDict[id])
        //                .OrderByDescending(a => a.PublishedAt)
        //                .Take(5)
        //                .ToList();

        //            if (!mergedArticles.Any())
        //                continue;

        //            model.CategorySections.Add(new CategorySectionVM
        //            {
        //                CategoryName = category.Name,
        //                CategorySlug = category.Slug,
        //                Articles = _mapper.Map<List<PublicArticleVM>>(mergedArticles)
        //            });
        //        }

        //        return model;
        //    });

        //    ViewBag.Type = type;
        //    return View(vm);
        //}

        public async Task<IActionResult> Index(string type = "today")
        {
            var cacheKey = CacheKeys.HomePage + "_Index";

            if (!_cache.TryGetValue(cacheKey, out HomePageVM vm))
            {
                await _homeCacheLock.WaitAsync();
                try
                {
                    // someone else may have already rebuilt it while we were waiting
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

            // ============================================================
            // 1. LOAD EDITORIAL CATEGORY CONFIGURATION
            // ------------------------------------------------------------
            // Load the complete configuration once.
            //
            // GetAllAsync() is cached by the service for 30 minutes, so
            // this does not result in a database query on every homepage
            // rebuild.
            //
            // The same configuration dictionary is then reused for
            // Top Story, Secondary Stories and Featured Stories.
            // ============================================================

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


            // ============================================================
            // 2. TOP STORY
            // ------------------------------------------------------------
            // Editorially pinned Top Story always has priority.
            //
            // If no story is pinned:
            //   - Use categories configured for Top Story.
            //   - If configured categories contain no published article,
            //     fall back to the previous GetTopStoryAsync() behaviour.
            //   - If no categories are configured, also preserve the
            //     previous behaviour.
            // ============================================================

            var pinnedTop =
                await _editorialPlacementService
                    .GetPinnedTopStoryAsync();

            PublicArticleVM? topStory;

            if (pinnedTop != null)
            {
                // Pinned editorial story always wins.
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
                    // Query all configured categories in one database query.
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
                        // Configuration exists, but currently there is no
                        // published article in those categories.
                        //
                        // Preserve the previous homepage behaviour.
                        topStory =
                            _mapper.Map<PublicArticleVM>(
                                await _articleService
                                    .GetTopStoryAsync());
                    }
                }
                else
                {
                    // No category configuration exists yet.
                    //
                    // Preserve the previous homepage behaviour.
                    topStory =
                        _mapper.Map<PublicArticleVM>(
                            await _articleService
                                .GetTopStoryAsync());
                }
            }

            model.TopStory = topStory;


            // ============================================================
            // 3. SECONDARY STORIES
            // ------------------------------------------------------------
            // If categories are configured in Editorial Control:
            //     use only those categories for organic Secondary Stories.
            //
            // If no categories are configured:
            //     preserve GetSecondaryStoriesAsync(50).
            //
            // Pinned Secondary Stories are handled separately below.
            // ============================================================

            var secondaryCategoryIds =
                GetConfiguredCategoryIds(
                    EditorialPlacementKeys.HomepageSecondaryStory);

            List<PublicArticleVM> secondaryAll;

            if (secondaryCategoryIds.Count > 0)
            {
                // One database query for all configured category IDs.
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
                // No configuration yet.
                //
                // Preserve existing behaviour.
                var secondaryRaw =
                    await _articleService
                        .GetSecondaryStoriesAsync(50);

                secondaryAll =
                    _mapper.Map<List<PublicArticleVM>>(
                        secondaryRaw);
            }


            // ============================================================
            // 4. LOAD PINNED EDITORIAL STORIES
            // ------------------------------------------------------------
            // Pinned stories remain completely independent of category
            // configuration.
            //
            // Existing editorial pinning behaviour is preserved.
            // ============================================================

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


            // ============================================================
            // 5. BUILD USED ARTICLE SET
            // ------------------------------------------------------------
            // Pinned articles must not be selected again as organic stories.
            //
            // Top Story is also excluded from organic selections.
            // ============================================================

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


            // ============================================================
            // 6. BUILD SECONDARY STORIES
            // ------------------------------------------------------------
            // Pinned Secondary Stories appear first.
            //
            // Organic Secondary Stories come from the configured categories
            // and exclude articles already used by pinned editorial areas.
            // ============================================================

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


            // ============================================================
            // 7. FEATURED STORIES
            // ------------------------------------------------------------
            // Organic Featured Stories now come from categories configured
            // in Editorial Control.
            //
            // The old hardcoded priorityCategorySlugs[] logic is completely
            // removed from the Razor view.
            //
            // Pinned Featured Stories remain independent and retain priority.
            // ============================================================

            var featuredCategoryIds =
                GetConfiguredCategoryIds(
                    EditorialPlacementKeys.HomepageFeaturedStory);

            List<PublicArticleVM> configuredFeaturedPool;

            if (featuredCategoryIds.Count > 0)
            {
                // One database query for all configured Featured categories.
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
                // No Featured category configuration yet.
                //
                // Preserve the previous general article pool behaviour.
                configuredFeaturedPool =
                    secondaryAll;
            }


            // ------------------------------------------------------------
            // IMPORTANT:
            //
            // Keep a reasonably large candidate pool here rather than
            // immediately taking only 10.
            //
            // The Razor view still removes articles that are already being
            // displayed in Top/Latest areas. Keeping 50 candidates gives
            // the view enough articles to fill the Featured slots after
            // duplicate filtering.
            // ------------------------------------------------------------

            var organicFeatured =
                configuredFeaturedPool
                    .Where(a => !pinnedIds.Contains(a.Id))
                    .ToList();


            // Pinned Featured Stories remain available separately.
            //
            // The Razor view combines:
            //
            //     pinnedFeatured + organicFeatured
            //
            // while applying its existing duplicate/slot logic.
            model.FeaturedStories =
                organicFeatured;


            // ============================================================
            // 8. HOMEPAGE CATEGORY SECTIONS
            // ------------------------------------------------------------
            // INTENTIONALLY UNCHANGED.
            //
            // Editorial Control configuration for:
            //
            //     Top Story
            //     Secondary Stories
            //     Featured Stories
            //
            // is completely separate from the lower homepage category
            // section ordering.
            //
            // Therefore the existing displayOrder remains unchanged.
            // ============================================================

            var categories =
                await _categoryService
                    .GetParentCategoriesWithChildrenAsync();


            // Build:
            //
            // Parent Category ID
            //        ↓
            // [Parent ID + Subcategory IDs]
            //
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


            // Fetch articles for all category/subcategory IDs
            // in one database call.
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


            // This ordering is ONLY for the lower homepage category
            // sections.
            //
            // It is NOT related to Editorial Control configuration.
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


            // Build each homepage category section.
            foreach (var category in orderedCategories)
            {
                // Include parent category and its subcategories.
                var relevantIds =
                    categoryIdMap[category.Id];

                // Merge articles from parent/subcategories,
                // newest first, keeping five.
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

                // Do not display empty sections.
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


            // ============================================================
            // 9. RETURN COMPLETE HOMEPAGE MODEL
            // ============================================================

            return model;
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
        [Route("/error/{code:int}")]
        public IActionResult StatusCodeError(int code)
        {
            Response.StatusCode = code; // ensure the response still reports the correct code

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
