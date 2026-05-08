using BolNews.Domain.Entities;
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
        public async Task<IActionResult> Index()
        {
            var users = await _userAdminService.GetUsersWithRolesAsync();
            return View(users);
        }
        public async Task<IActionResult> AssignRole(string id)
        {
            var data = await _userAdminService.GetAssignRoleDataAsync(id);
            if (data.user == null)
                return NotFound();

            ViewBag.Roles = data.roles;
            ViewBag.UserRoles = data.userRoles;

            return View(data.user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            await _userAdminService.AssignSingleRoleAsync(userId, role);
            return RedirectToAction("Index");
        }
    }

}
