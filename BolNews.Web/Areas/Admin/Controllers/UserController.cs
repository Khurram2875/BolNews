using BolNews.Domain.Entities;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using BolNews.Domain.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IUserAdminService _userAdminService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly IReadOnlyDictionary<string, string> RoleDescriptions =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [Roles.Admin] = "Full CMS administration, including users, roles, publisher settings, and all newsroom areas.",
                [Roles.Editor] = "Editorial management, including dashboard, categories, reporters, article workflow, and editorial controls.",
                [Roles.SubEditor] = "Sub-editing, article workflow, dashboard, breaking news, and editorial placements.",
                [Roles.Author] = "Article authoring and assigned story work, with access to the media library.",
                [Roles.Factchecker] = "Fact-check assignments and the fact-check stages of the editorial workflow.",
                [Roles.User] = "Public account access only; this role does not grant newsroom administration access."
            };

        public UserController(
            IUserAdminService userAdminService,
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            _userAdminService = userAdminService;
            _roleManager = roleManager;
            _userManager = userManager;
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

        [ActionName("Roles")]
        public async Task<IActionResult> RoleCatalog()
        {
            var existingRoles = await _roleManager.Roles
                .Where(role => role.Name != null)
                .Select(role => role.Name!)
                .ToListAsync();

            var model = new RoleManagementVM();
            foreach (var roleName in Roles.SupportedRoleNames)
            {
                var isCreated = existingRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
                var userCount = isCreated
                    ? (await _userManager.GetUsersInRoleAsync(roleName)).Count
                    : 0;

                model.Roles.Add(new SupportedRoleVM
                {
                    Name = roleName,
                    Description = RoleDescriptions[roleName],
                    IsCreated = isCreated,
                    AssignedUserCount = userCount
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            var supportedRole = Roles.SupportedRoleNames.FirstOrDefault(
                role => string.Equals(role, roleName, StringComparison.OrdinalIgnoreCase));

            if (supportedRole == null)
                return BadRequest();

            if (await _roleManager.RoleExistsAsync(supportedRole))
            {
                TempData["Success"] = $"The {supportedRole} role already exists.";
                return RedirectToAction("Roles");
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(supportedRole));
            if (result.Succeeded)
            {
                TempData["Success"] = $"The {supportedRole} role was created.";
            }
            else
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(error => error.Description));
            }

            return RedirectToAction("Roles");
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
