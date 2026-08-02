using BolNews.Application.Interfaces;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Editor + "," + Roles.SubEditor)]
    public class EditorialPlacementsController : Controller
    {
        private readonly IEditorialPlacementService _placementService;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditorialPlacementsController(
            IEditorialPlacementService placementService,
            UserManager<ApplicationUser> userManager)
        {
            _placementService = placementService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var topStory = await _placementService.GetPinnedTopStoryAsync();
            var secondaryStories = await _placementService.GetPinnedSecondaryStoriesAsync();
            var latestStories = await _placementService.GetPinnedLatestStoriesAsync();
            var featuredStories = await _placementService.GetPinnedFeaturedStoriesAsync();

            var model = new EditorialPlacementIndexVM
            {
                TopStory = topStory == null ? null : MapPlacement(topStory),
                SecondaryStories = secondaryStories.Select(MapPlacement).ToList(),
                LatestStories = latestStories.Select(MapPlacement).ToList(),
                FeaturedStories = featuredStories.Select(MapPlacement).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PinTopStory(int articleId, string? returnUrl = null)
        {
            return await ExecutePlacementActionAsync(
                roles => _placementService.PinTopStoryAsync(articleId, _userManager.GetUserId(User)!, roles),
                "Top story pinned.",
                returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnpinTopStory(string? returnUrl = null)
        {
            return await ExecutePlacementActionAsync(
                roles => _placementService.UnpinTopStoryAsync(_userManager.GetUserId(User)!, roles),
                "Top story unpinned.",
                returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PinSecondaryStory(int articleId, string? returnUrl = null)
        {
            return await ExecutePlacementActionAsync(
                roles => _placementService.PinSecondaryStoryAsync(articleId, _userManager.GetUserId(User)!, roles),
                "Secondary story pinned.",
                returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnpinSecondaryStory(int placementId, string? returnUrl = null)
        {
            return await ExecutePlacementActionAsync(
                roles => _placementService.UnpinSecondaryStoryAsync(placementId, _userManager.GetUserId(User)!, roles),
                "Secondary story removed.",
                returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveSecondaryStory(int placementId, int direction, string? returnUrl = null)
        {
            return await ExecutePlacementActionAsync(
                roles => _placementService.MoveSecondaryStoryAsync(placementId, direction, _userManager.GetUserId(User)!, roles),
                "Secondary story order updated.",
                returnUrl);
        }

        private async Task<IActionResult> ExecutePlacementActionAsync(
            Func<IList<string>, Task> action,
            string successMessage,
            string? returnUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                await action(roles);
                TempData["Success"] = successMessage;
            }
            catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                TempData["Error"] = ex.Message;
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        private static EditorialPlacementVM MapPlacement(EditorialPlacement placement)
            => new()
            {
                PlacementId = placement.Id,
                ArticleId = placement.ArticleId,
                Title = placement.Article?.Title ?? "Unknown article",
                CategoryName = placement.Article?.Category?.Name ?? "",
                PublishedAt = placement.Article?.PublishedAt,
                SortOrder = placement.SortOrder
            };

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PinLatestStory(int articleId, string? returnUrl = null)
        {
            return await ExecutePlacementActionAsync(
                roles => _placementService.PinLatestStoryAsync(articleId, _userManager.GetUserId(User)!, roles),
                "Latest story pinned.",
                returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnpinLatestStory(int placementId, string? returnUrl = null)
        {
            return await ExecutePlacementActionAsync(
                roles => _placementService.UnpinLatestStoryAsync(placementId, _userManager.GetUserId(User)!, roles),
                "Latest story removed.",
                returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveLatestStory(int placementId, int direction, string? returnUrl = null)
        {
            return await ExecutePlacementActionAsync(
                roles => _placementService.MoveLatestStoryAsync(placementId, direction, _userManager.GetUserId(User)!, roles),
                "Latest story order updated.",
                returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PinFeaturedStory(int articleId, string? returnUrl = null)
        {
            return await ExecutePlacementActionAsync(
                roles => _placementService.PinFeaturedStoryAsync(articleId, _userManager.GetUserId(User)!, roles),
                "Featured story pinned.",
                returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnpinFeaturedStory(int placementId, string? returnUrl = null)
        {
            return await ExecutePlacementActionAsync(
                roles => _placementService.UnpinFeaturedStoryAsync(placementId, _userManager.GetUserId(User)!, roles),
                "Featured story removed.",
                returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveFeaturedStory(int placementId, int direction, string? returnUrl = null)
        {
            return await ExecutePlacementActionAsync(
                roles => _placementService.MoveFeaturedStoryAsync(placementId, direction, _userManager.GetUserId(User)!, roles),
                "Featured story order updated.",
                returnUrl);
        }
    }
}
