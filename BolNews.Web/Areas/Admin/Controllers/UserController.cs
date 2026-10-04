using BolNews.Domain.Entities;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IUserAdminService _userAdminService;

        public UserController(IUserAdminService userAdminService)
        {
            _userAdminService = userAdminService;
        }
        public async Task<IActionResult> Index(string status = "active")
        {
            var normalizedStatus = status.ToLowerInvariant() switch
            {
                "inactive" => "inactive",
                "all" => "all",
                _ => "active"
            };

            bool? isActive = normalizedStatus switch
            {
                "inactive" => false,
                "all" => null,
                _ => true
            };

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var users = await _userAdminService.GetUsersWithRolesAsync(isActive, currentUserId);
            ViewData["UserStatusFilter"] = normalizedStatus;
            return View(users);
        }
        public async Task<IActionResult> AssignRole(string id)
        {
            var data = await _userAdminService.GetAssignRoleDataAsync(id);

            if (data.user == null)
                return NotFound();

            var model = new AssignRolesVM
            {
                UserId = data.user.Id,
                Email = data.user.Email ?? "",
                Roles = data.roles.Select(r => new RoleSelectionVM
                {
                    RoleName = r,
                    IsSelected = data.userRoles.Contains(r)
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(string userId, bool isActive, string status = "active")
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!isActive && string.Equals(currentUserId, userId, StringComparison.Ordinal))
            {
                TempData["Error"] = "You cannot deactivate your own administrator account.";
                return RedirectToAction(nameof(Index), new { status });
            }

            if (!await _userAdminService.SetUserActiveAsync(userId, isActive))
                return NotFound();

            TempData["Success"] = isActive ? "User activated." : "User deactivated.";
            return RedirectToAction(nameof(Index), new { status });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, AssignRolesVM model)
        {
            var selectedRoles = model.Roles
            .Where(r => r.IsSelected)
            .Select(r => r.RoleName)
            .ToList();

            await _userAdminService.UpdateUserRolesAsync(
                model.UserId,
                selectedRoles);

            return RedirectToAction(nameof(Index));
        }
    }

}
