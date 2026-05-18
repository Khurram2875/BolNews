using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationBellViewComponent(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity.IsAuthenticated)
                return View(new List<NotificationVM>());

            var user =
                await _userManager.GetUserAsync(HttpContext.User);

            if (user == null)
                return View(new List<NotificationVM>());

            var notifications =
                await _notificationService.GetUnreadAsync(user.Id);

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

            return View(vm);
        }
    }
}
