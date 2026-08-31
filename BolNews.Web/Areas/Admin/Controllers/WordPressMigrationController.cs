using BolNews.Application.Interfaces;
using BolNews.Infrastructure.Services.WordPressMigration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class WordPressMigrationController : Controller
    {
        private readonly IWordPressArticleReader _reader;
        private readonly IWordPressAuthorResolver _authorResolver;
        private readonly IWordPressCategoryResolver _categoryResolver;
        private readonly IWordPressReporterResolver _reporterResolver;
        private readonly IWordPressArticleImportService _importService;
        private readonly WordPressMigrationRepairService _repairService;
        private readonly ILogger<WordPressMigrationController> _logger;
        private readonly WordPressMigrationState _migrationState;
        private readonly IWordPressMigrationQueue _migrationQueue;
        
        public WordPressMigrationController(
            IWordPressArticleReader reader, IWordPressAuthorResolver authorResolver, 
            IWordPressCategoryResolver categoryResolver,IWordPressReporterResolver reporterResolver, 
            IWordPressArticleImportService importService, WordPressMigrationRepairService repairService, 
            ILogger<WordPressMigrationController> logger, WordPressMigrationState migrationState, 
            IWordPressMigrationQueue migrationQueue)
        {
            _reader = reader;
            _authorResolver = authorResolver;
            _categoryResolver = categoryResolver;
            _reporterResolver = reporterResolver;
            _importService = importService;
            _repairService = repairService;
            _logger = logger;
            _migrationState = migrationState;
            _migrationQueue = migrationQueue;
        }

        public async Task<IActionResult> Test()
        {
            var fromDate = new DateTime(2026, 6, 1);
            var toDate = new DateTime(2026, 7, 1);

            var articles = await _reader.GetArticlesAsync(
                fromDate,
                toDate);

            return Json(new
            {
                count = articles.Count,
                articles = articles.Take(5)
            });
        }
        [HttpGet]
        public async Task<IActionResult> TestAuthor(
    int wordpressAuthorId,
    string authorName)
        {
            var author = await _authorResolver.ResolveAsync(
                wordpressAuthorId,
                authorName);

            if (author == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Author could not be resolved."
                });
            }

            return Json(new
            {
                success = true,
                authorId = author.Id,
                authorName = author.Name,
                authorSlug = author.Slug,
                userId = author.UserId
            });
        }
        [HttpGet]
        public async Task<IActionResult> TestCategoryReporter()
        {
            var articles = await _reader.GetArticlesAsync(
                new DateTime(2026, 6, 1),
                new DateTime(2026, 7, 1));

            var wpArticle = articles.FirstOrDefault();

            if (wpArticle == null)
            {
                return Json(new
                {
                    success = false,
                    message = "No WordPress articles found."
                });
            }

            var category =
                await _categoryResolver.ResolveAsync(
                    wpArticle.WordPressCategoryId,
                    wpArticle.CategoryName,
                    wpArticle.CategorySlug);

            var reporters =
                await _reporterResolver.ResolveAsync(
                    wpArticle.Reporters);

            return Json(new
            {
                success = true,

                wordpressPostId =
                    wpArticle.WordPressPostId,

                category = category == null
                    ? null
                    : new
                    {
                        id = category.Id,
                        name = category.Name,
                        slug = category.Slug
                    },

                reporters = reporters.Select(r => new
                {
                    id = r.Id,
                    name = r.Name,
                    slug = r.Slug
                }).ToList()
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(
    DateTime fromDate,
    DateTime toDate,
    int? take)
        {
            if (fromDate >= toDate)
            {
                TempData["Error"] =
                    "From Date must be earlier than To Date.";

                return RedirectToAction(nameof(Index));
            }

            if (_migrationState.IsRunning)
            {
                TempData["Error"] =
                    "Another migration operation is already running.";

                return RedirectToAction(nameof(Index));
            }

            var currentUserId =
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                TempData["Error"] =
                    "Unable to determine the current user.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                _migrationState.Start("Import");

                await _migrationQueue.QueueAsync(
                    new WordPressMigrationJob(
                        WordPressMigrationOperation.Import,
                        fromDate,
                        toDate,
                        currentUserId,
                        take));

                TempData["Success"] =
                    "WordPress import started in the background.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to start WordPress import.");

                TempData["Error"] =
                    "Unable to start WordPress import.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.MigrationState =
                _migrationState;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RepairFeaturedImages(
      DateTime fromDate,
      DateTime toDate)
        {
            if (fromDate >= toDate)
            {
                TempData["Error"] =
                    "From Date must be earlier than To Date.";

                return RedirectToAction(nameof(Index));
            }

            if (_migrationState.IsRunning)
            {
                TempData["Error"] =
                    "Another migration operation is already running.";

                return RedirectToAction(nameof(Index));
            }

            _migrationState.Start(
                "Repair Featured Images");

            await _migrationQueue.QueueAsync(
                new WordPressMigrationJob(
                    WordPressMigrationOperation.RepairFeaturedImages,
                    fromDate,
                    toDate));

            TempData["Success"] =
                "Featured image repair started in the background.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NormalizeContent(
    DateTime fromDate,
    DateTime toDate)
        {
            if (fromDate >= toDate)
            {
                TempData["Error"] =
                    "From Date must be earlier than To Date.";

                return RedirectToAction(nameof(Index));
            }

            if (_migrationState.IsRunning)
            {
                TempData["Error"] =
                    "Another migration operation is already running.";

                return RedirectToAction(nameof(Index));
            }

            _migrationState.Start(
                "Normalize Content");

            await _migrationQueue.QueueAsync(
                new WordPressMigrationJob(
                    WordPressMigrationOperation.NormalizeContent,
                    fromDate,
                    toDate));

            TempData["Success"] =
                "Content normalization started in the background.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessInlineMedia(
     DateTime fromDate,
     DateTime toDate)
        {
            if (fromDate >= toDate)
            {
                TempData["Error"] =
                    "From Date must be earlier than To Date.";

                return RedirectToAction(nameof(Index));
            }

            if (_migrationState.IsRunning)
            {
                TempData["Error"] =
                    "Another migration operation is already running.";

                return RedirectToAction(nameof(Index));
            }

            _migrationState.Start(
                "Process Inline Media");

            await _migrationQueue.QueueAsync(
                new WordPressMigrationJob(
                    WordPressMigrationOperation.ProcessInlineMedia,
                    fromDate,
                    toDate));

            TempData["Success"] =
                "Inline media processing started in the background.";

            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Abort()
        {
            if (!_migrationState.IsRunning)
            {
                TempData["Error"] =
                    "No migration operation is currently running.";

                return RedirectToAction(nameof(Index));
            }

            _logger.LogWarning(
                "Abort requested. Operation={Operation}",
                _migrationState.Operation);

            _migrationState.RequestAbort();

            TempData["Success"] =
                "Abort requested. The operation will stop shortly.";

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public IActionResult Status()
        {
            return Json(new
            {
                isRunning = _migrationState.IsRunning,
                operation = _migrationState.Operation,
                status = _migrationState.Status,

                startTime = _migrationState.StartTime,
                finishTime = _migrationState.FinishTime,

                total = _migrationState.Total,
                imported = _migrationState.Imported,
                repaired = _migrationState.Repaired,
                skipped = _migrationState.Skipped,
                failed = _migrationState.Failed
            });
        }
    }
}
