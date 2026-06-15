using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        // GET
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Invalid login attempt");
                return View();
            }
            

            var result = await _signInManager.PasswordSignInAsync(
                email,
                password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in successfully.", email);

                var user = await _userManager.FindByNameAsync(email);

                var roles = await _userManager.GetRolesAsync(user);

                // Editorial team gets Editorial Dashboard
                if (roles.Contains(Roles.Editor) ||
                    roles.Contains(Roles.SubEditor) ||
                    roles.Contains(Roles.Factchecker))
                {
                    return RedirectToAction(
                        "Index",
                        "Editorial",
                        new { area = "Admin" });
                }

                // Author-only users go to Articles
                if (roles.Contains(Roles.Author))
                {
                    return RedirectToAction(
                        actionName: "Index",
                        controllerName: "Articles",
                        routeValues: new { area = "Admin" });
                }

                // Admin goes to Admin Dashboard
                if (roles.Contains(Roles.Admin))
                {
                    return RedirectToAction(
                        actionName: "Index",
                        controllerName: "Dashboard",
                        routeValues: new { area = "Admin" });
                }

                // Normal users / no roles
                return RedirectToAction(
                    actionName: "Index",
                    controllerName: "Home",
                    routeValues: new { area = "" });
            }

            ModelState.AddModelError("", "Invalid email or password");

            _logger.LogWarning("Failed login attempt for {Email}.", email);

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("User {User} logged out.", User.Identity?.Name);

            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home", new { area = "" });
        }
        public IActionResult AccessDenied()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }
    }
}
