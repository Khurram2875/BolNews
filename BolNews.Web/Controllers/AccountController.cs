using BolNews.Application.Interfaces;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Infrastructure.Services;
using BolNews.Web.Models;
using BolNews.Web.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

namespace BolNews.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailService _emailService;
        private readonly EmailOptions _emailOptions;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger,
            IEmailService emailService,
             IOptions<EmailOptions> emailOptions)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailService = emailService;
            _emailOptions = emailOptions.Value; 
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
                _logger.LogInformation(
                    "User {Email} logged in successfully.",
                    email);

                var user = await _userManager.FindByNameAsync(email);

                var roles = await _userManager.GetRolesAsync(user);

                // Admin and Editor go to the Admin Dashboard
                if (roles.Contains(Roles.Admin) ||
                    roles.Contains(Roles.Editor))
                {
                    return RedirectToAction(
                        actionName: "Index",
                        controllerName: "Dashboard",
                        routeValues: new { area = "Admin" });
                }

                // SubEditor and Author go directly to Articles
                if (roles.Contains(Roles.SubEditor) ||
                    roles.Contains(Roles.Author))
                {
                    return RedirectToAction(
                        actionName: "Index",
                        controllerName: "Articles",
                        routeValues: new { area = "Admin" });
                }

                // Factchecker keeps the Editorial Dashboard
                if (roles.Contains(Roles.Factchecker))
                {
                    return RedirectToAction(
                        actionName: "Index",
                        controllerName: "Editorial",
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
        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new ChangePasswordVM());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                await _signInManager.SignOutAsync();

                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword);

            if (result.Succeeded)
            {
                // Refresh the authentication cookie after a successful
                // password change.
                await _signInManager.RefreshSignInAsync(user);

                _logger.LogInformation(
                    "User {UserId} changed their password successfully.",
                    user.Id);

                TempData["Success"] =
                    "Your password has been changed successfully.";

                return RedirectToAction(nameof(ChangePassword));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }
        // ============================================================
        // FORGOT PASSWORD
        // ============================================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordVM());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user =
                await _userManager.FindByEmailAsync(
                    model.Email.Trim());

            // IMPORTANT:
            // Always show the same response whether the email exists
            // or not. This prevents account/email enumeration.
            if (user == null)
            {
                return RedirectToAction(
                    nameof(ForgotPasswordConfirmation));
            }

            var token =
                await _userManager
                    .GeneratePasswordResetTokenAsync(user);

            var encodedToken =
                Microsoft.AspNetCore.WebUtilities.WebEncoders
                    .Base64UrlEncode(
                        System.Text.Encoding.UTF8.GetBytes(token));

            var resetUrl =
                Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new
                    {
                        code = encodedToken,
                        email = user.Email
                    },
                    _emailOptions.PublicBaseUrl);

            if (string.IsNullOrWhiteSpace(resetUrl))
            {
                _logger.LogError(
                    "Unable to generate password reset URL for user {UserId}.",
                    user.Id);

                return RedirectToAction(
                    nameof(ForgotPasswordConfirmation));
            }

            var emailBody = $"""
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset="utf-8">
                        <title>Password Reset</title>
                    </head>
                    <body style="margin:0;padding:0;background:#f5f5f5;font-family:Arial,sans-serif;">
                        <div style="max-width:600px;margin:40px auto;background:#ffffff;
                                    padding:40px;border-radius:8px;">
            
                            <h2 style="margin-top:0;color:#111;">
                                BOL News Password Reset
                            </h2>

                            <p>
                                We received a request to reset the password for your
                                BOL News account.
                            </p>

                            <p>
                                Click the button below to choose a new password:
                            </p>

                            <p style="margin:30px 0;">
                                <a href="{System.Net.WebUtility.HtmlEncode(resetUrl)}"
                                   style="display:inline-block;padding:12px 24px;
                                          background:#111;color:#fff;text-decoration:none;
                                          border-radius:5px;">
                                    Reset Password
                                </a>
                            </p>

                            <p>
                                This link is temporary and can only be used once.
                            </p>

                            <p>
                                If you did not request a password reset, you can safely
                                ignore this email.
                            </p>

                            <hr style="border:0;border-top:1px solid #eee;margin:30px 0;">

                            <p style="font-size:12px;color:#777;">
                                BOL News
                            </p>
                        </div>
                    </body>
                    </html>
                    """;

            try
            {
                await _emailService.SendAsync(
                    user.Email!,
                    "BOL News - Password Reset",
                    emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send password reset email for user {UserId}.",
                    user.Id);
            }

            _logger.LogInformation(
                "Password reset requested for user {UserId}.",
                user.Id);

            return RedirectToAction(
                nameof(ForgotPasswordConfirmation));
        }


        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }
        // ============================================================
        // RESET PASSWORD
        // ============================================================

        [HttpGet]
        public IActionResult ResetPassword(
            string code,
            string email)
        {
            if (string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            var model = new ResetPasswordVM
            {
                Code = code,
                Email = email
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user =
                await _userManager.FindByEmailAsync(
                    model.Email.Trim());

            // Do not reveal whether the account exists.
            if (user == null)
            {
                return RedirectToAction(
                    nameof(ResetPasswordConfirmation));
            }

            string decodedToken;

            try
            {
                var tokenBytes =
                    Microsoft.AspNetCore.WebUtilities.WebEncoders
                        .Base64UrlDecode(model.Code);

                decodedToken =
                    System.Text.Encoding.UTF8.GetString(
                        tokenBytes);
            }
            catch (FormatException)
            {
                _logger.LogWarning(
                    "Invalid password reset token format submitted for {Email}.",
                    model.Email);

                ModelState.AddModelError(
                    string.Empty,
                    "The password reset link is invalid or has expired.");

                return View(model);
            }

            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    decodedToken,
                    model.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "Password was reset successfully for user {UserId}.",
                    user.Id);

                return RedirectToAction(
                    nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }


        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}
