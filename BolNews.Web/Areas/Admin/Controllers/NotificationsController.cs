using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var notifications =
                await _notificationService.GetUnreadAsync(user.Id);

            var vm = notifications.Select(x => new NotificationVM
            {
                Id = x.Id,
                Title = x.Title,
                Message = x.Message,
                CreatedAt = x.CreatedAt,
                Url = x.Url
            }).ToList();

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Feed()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var notifications = await _notificationService.GetUnreadAsync(user.Id);

            var vm = notifications
                .Take(10)
                .Select(x => new NotificationVM
                {
                    Id = x.Id,
                    Title = x.Title,
                    Message = x.Message,
                    CreatedAt = x.CreatedAt,
                    Url = x.Url
                })
                .ToList();

            return Json(new
            {
                unreadCount = notifications.Count,
                items = vm.Select(x => new
                {
                    id = x.Id,
                    title = x.Title,
                    message = x.Message,
                    createdAt = x.CreatedAt.ToString("g"),
                    url = x.Url
                })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Open(int id, string? url)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            await _notificationService.MarkReadAsync(id, user.Id);

            if (!string.IsNullOrWhiteSpace(url) && Url.IsLocalUrl(url))
                return LocalRedirect(url);

            return RedirectToAction(nameof(Index));
        }
    }
}
