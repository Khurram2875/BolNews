using BolNews.Application.Interfaces;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =
    Roles.Admin + "," +
    Roles.Editor + "," +
    Roles.SubEditor + "," +
    Roles.Factchecker)]
    public class EditorialController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOperationalAnalyticsService _analyticsService;

        public EditorialController(IArticleService articleService, UserManager<ApplicationUser> userManager, IOperationalAnalyticsService analyticsService)
        {
            _articleService = articleService;
            _userManager = userManager;
            _analyticsService = analyticsService;
        }

        public async Task<IActionResult> Index(string filter = "all")
        {
            var user = await _userManager.GetUserAsync(User);
            var currentUserId = _userManager.GetUserId(User);
            var roles = await _userManager.GetRolesAsync(user);
            var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            var editors = await _userManager.GetUsersInRoleAsync(Roles.Editor);
            var subEditors = await _userManager.GetUsersInRoleAsync(Roles.SubEditor);
            var factChecker = await _userManager.GetUsersInRoleAsync(Roles.Factchecker);

            var queue = await _articleService.GetEditorialQueueAsync(roles);
            switch (filter.ToLower())
            {
                case "mine":
                    queue = queue
                        .Where(x =>
                            x.ReviewerUserId == user.Id ||
                            x.FactCheckerUserId == user.Id)
                        .ToList();
                    break;

                case "unassigned":
                    queue = queue
                        .Where(x =>
                            string.IsNullOrWhiteSpace(x.ReviewerUserId) ||
                            string.IsNullOrWhiteSpace(x.FactCheckerUserId))
                        .ToList();
                    break;
            }
            var vm = new EditorialDashboardVM
            {
                Submitted = queue
                        .Where(x => x.WorkflowStatus == ArticleWorkflowStatus.Submitted)
                        .ToList(),

                UnderReview = queue
                        .Where(x => x.WorkflowStatus == ArticleWorkflowStatus.UnderReview)
                        .ToList(),

                FactCheckPending = queue
                        .Where(x => x.WorkflowStatus == ArticleWorkflowStatus.FactCheckPending)
                        .ToList(),

                MyFactChecks = queue
                        .Where(x => x.WorkflowStatus == ArticleWorkflowStatus.FactCheckPending
                        && x.FactCheckerUserId == user.Id)
                        .ToList(),

                Approved = queue
                        .Where(x => x.WorkflowStatus == ArticleWorkflowStatus.Approved)
                        .ToList(),

                PublishedToday = 0, // temporary placeholder

                Analytics = await _analyticsService.GetAnalyticsAsync()

            };

            vm.MyFactChecks = vm.FactCheckPending
              .Where(x => x.FactCheckerUserId == currentUserId)
              .ToList();

            var editorialUsers = admins
                .Concat(editors)
                .Concat(subEditors)
                .Concat(factChecker)
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToList();
            ViewBag.EditorialUsers = editorialUsers;
            ViewBag.CurrentFilter = filter;
            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transition(int articleId, ArticleWorkflowStatus targetStatus, string? reason = null, DateTime? scheduledPublishAt = null,
    DateTime? embargoUntil = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            if (scheduledPublishAt.HasValue)
            {
                scheduledPublishAt = DateTime.SpecifyKind(
                    scheduledPublishAt.Value,
                    DateTimeKind.Local)
                    .ToUniversalTime();
            }

            if (embargoUntil.HasValue)
            {
                embargoUntil = DateTime.SpecifyKind(
                    embargoUntil.Value,
                    DateTimeKind.Local)
                    .ToUniversalTime();
            }
            if (scheduledPublishAt.HasValue && embargoUntil.HasValue)
            {
                TempData["Error"] =
                    "Choose either Scheduled Publish OR Embargo, not both.";

                return RedirectToAction(nameof(Index));
            }
            await _articleService.TransitionWorkflowAsync(
                articleId,
                targetStatus,
                user.Id,
                roles,
                reason, 
                scheduledPublishAt,
                embargoUntil);

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignReviewer(int articleId, string reviewerUserId)
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            await _articleService.AssignReviewerAsync(
                articleId,
                reviewerUserId,
                user.Id,
                roles);

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignFactChecker(int articleId, string factCheckerUserId)
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            await _articleService.AssignFactCheckerAsync(
                articleId,
                factCheckerUserId,
                user.Id,
                roles);

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSchedule(int articleId)
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            await _articleService.CancelScheduleAsync(
                articleId,
                user.Id,
                roles);

            return RedirectToAction(nameof(Index));
        }
    }
}
