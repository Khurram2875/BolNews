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
        public WordPressMigrationController(
            IWordPressArticleReader reader, IWordPressAuthorResolver authorResolver, IWordPressCategoryResolver categoryResolver,
    IWordPressReporterResolver reporterResolver, IWordPressArticleImportService importService, WordPressMigrationRepairService repairService, ILogger<WordPressMigrationController> logger, WordPressMigrationState migrationState)
        {
            _reader = reader;
            _authorResolver = authorResolver;
            _categoryResolver = categoryResolver;
            _reporterResolver = reporterResolver;
            _importService = importService;
            _repairService = repairService;
            _logger = logger;
            _migrationState = migrationState;
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
    int? take,
    CancellationToken cancellationToken)
        {
            var userId =
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            if (fromDate >= toDate)
            {
                ViewBag.Error =
                    "From Date must be earlier than To Date.";

                return View("Index");
            }

            if (take.HasValue && take.Value <= 0)
            {
                ViewBag.Error =
                    "Take must be greater than zero.";

                return View("Index");
            }

            var result =
                await _importService.ImportAsync(
                    fromDate,
                    toDate,
                    userId,
                    take,
                    cancellationToken);

            ViewBag.Result = result;

            return View("Index");
        }
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.MigrationState = _migrationState;

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RepairFeaturedImages(DateTime fromDate, DateTime toDate)
        {
            if (_migrationState.IsRunning)
            {
                TempData["Error"] =
                    "Another migration operation is already running.";

                return RedirectToAction(nameof(Index));
            }

            if (fromDate >= toDate)
            {
                TempData["Error"] =
                    "From Date must be earlier than To Date.";

                return RedirectToAction(nameof(Index));
            }

            _migrationState.Start("Repair Featured Images");

            try
            {
                var result =
                    await _repairService.RepairFeaturedImagesAsync(
                        fromDate,
                        toDate,
                        _migrationState.Token);

                _migrationState.Complete(
                    result.Total,
                    result.Repaired,
                    result.Skipped,
                    result.Failed);

                ViewBag.RepairResult = result;
            }
            catch (OperationCanceledException)
            {
                _migrationState.MarkAborted(
                    0, 0, 0, 0);

                TempData["Error"] =
                    "Featured image processing was aborted.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Featured image repair failed.");

                _migrationState.MarkFailed(
                    0, 0, 0, 1);

                TempData["Error"] =
                    "Featured image repair failed.";

                return RedirectToAction(nameof(Index));
            }

            return View("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NormalizeContent(DateTime fromDate,DateTime toDate)
        {
            if (_migrationState.IsRunning)
            {
                TempData["Error"] =
                    "Another migration operation is already running.";

                return RedirectToAction(nameof(Index));
            }

            if (fromDate >= toDate)
            {
                TempData["Error"] =
                    "From Date must be earlier than To Date.";

                return RedirectToAction(nameof(Index));
            }

            _migrationState.Start("Normalize Content");

            try
            {
                var result =
                    await _repairService.NormalizeContentAsync(
                        fromDate,
                        toDate,
                        _migrationState.Token);

                _migrationState.Complete(
                    result.Total,
                    result.Repaired,
                    result.Skipped,
                    result.Failed);

                ViewBag.RepairResult = result;
            }
            catch (OperationCanceledException)
            {
                _migrationState.MarkAborted(
                    0, 0, 0, 0);

                TempData["Error"] =
                    "Content normalization was aborted.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Content normalization failed.");

                _migrationState.MarkFailed(
                    0, 0, 0, 1);

                TempData["Error"] =
                    "Content normalization failed.";

                return RedirectToAction(nameof(Index));
            }

            return View("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessInlineMedia(
    DateTime fromDate,
    DateTime toDate)
        {
            if (_migrationState.IsRunning)
            {
                TempData["Error"] =
                    "Another migration operation is already running.";

                return RedirectToAction(nameof(Index));
            }

            if (fromDate >= toDate)
            {
                TempData["Error"] =
                    "From Date must be earlier than To Date.";

                return RedirectToAction(nameof(Index));
            }

            _migrationState.Start("Process Inline Media");

            try
            {
                var result =
                    await _repairService.ProcessInlineMediaAsync(
                        fromDate,
                        toDate,
                        _migrationState.Token);

                _migrationState.Complete(
                    result.Total,
                    result.Repaired,
                    result.Skipped,
                    result.Failed);

                ViewBag.RepairResult = result;
            }
            catch (OperationCanceledException)
            {
                _migrationState.MarkAborted(
                    0, 0, 0, 0);

                TempData["Error"] =
                    "Inline media processing was aborted.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Inline media processing failed.");

                _migrationState.MarkFailed(
                    0, 0, 0, 1);

                TempData["Error"] =
                    "Inline media processing failed.";

                return RedirectToAction(nameof(Index));
            }

            return View("Index");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Abort()
        {
            if (_migrationState.IsRunning)
            {
                _logger.LogWarning(
                    "WordPress migration abort requested. Operation={Operation}",
                    _migrationState.Operation);

                _migrationState.Abort();

                TempData["Success"] =
                    "Abort requested. The current operation will stop shortly.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
