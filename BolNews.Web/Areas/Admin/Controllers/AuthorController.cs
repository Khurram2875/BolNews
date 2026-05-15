using AutoMapper;
using BolNews.Application.Common.Helpers;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class AuthorController : Controller
    {
        private readonly IAuthorService _authorService;
        private readonly IImageService _imageService;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthorController(
            IAuthorService authorService,
            IImageService imageService,
            IMapper mapper,
            IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _authorService = authorService;
            _imageService = imageService;
            _mapper = mapper;
            _env = env;
            _userManager = userManager;
        }

        // 🔹 LIST
        public async Task<IActionResult> Index()
        {
            var authors = await _authorService.GetAllAsync();
            var vm = _mapper.Map<List<AuthorVM>>(authors);
            return View(vm);
        }

        // 🔹 CREATE (GET)
        public IActionResult Create()
        {
            var users = _userManager.Users
            .Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FullName} ({u.Email})"
            })
            .ToList();

            var vm = new AuthorVM
            {
                ExistingUsers = users
            };

            return View(vm);
        }

        // 🔹 CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AuthorVM vm)
        {
            vm.ExistingUsers = _userManager.Users
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FullName} ({u.Email})"
                })
                .ToList();

            if (!ModelState.IsValid)
                return View(vm);

            ApplicationUser user;

            if (vm.LinkExistingUser)
            {
                if (string.IsNullOrWhiteSpace(vm.SelectedUserId))
                {
                    ModelState.AddModelError("SelectedUserId", "Please select a user.");
                    return View(vm);
                }

                user = await _userManager.FindByIdAsync(vm.SelectedUserId);

                if (user == null)
                {
                    ModelState.AddModelError("", "Selected user not found.");
                    return View(vm);
                }

                var existingAuthor = await _authorService.GetByUserIdAsync(user.Id);

                if (existingAuthor != null)
                {
                    ModelState.AddModelError("", "This user already has an author profile.");
                    return View(vm);
                }

                if (!await _userManager.IsInRoleAsync(user, "Author"))
                {
                    await _userManager.AddToRoleAsync(user, "Author");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(vm.Password))
                {
                    ModelState.AddModelError("Password", "Password is required.");
                    return View(vm);
                }

                user = new ApplicationUser
                {
                    UserName = vm.Email,
                    Email = vm.Email,
                    FullName = vm.Name
                };

                var result = await _userManager.CreateAsync(user, vm.Password);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);

                    return View(vm);
                }

                await _userManager.AddToRoleAsync(user, "Author");
            }

            var dto = new AuthorDto
            {
                Name = vm.Name,
                Bio = vm.Bio,
                UserId = user.Id
            };

            var author = await _authorService.CreateAsync(dto);

            if (vm.ImageFile != null)
            {
                if (!ImageValidator.IsValid(vm.ImageFile, out var error))
                {
                    ModelState.AddModelError("ImageFile", error);
                    return View(vm);
                }

                using var stream = vm.ImageFile.OpenReadStream();

                var imagePath = await _imageService.SaveAuthorImageAsync(
                    stream,
                    author.Id,
                    _env.WebRootPath
                );

                await _authorService.UpdateImageAsync(author.Id, imagePath);
            }

            return RedirectToAction(nameof(Index));
        }

        // 🔹 EDIT (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var author = await _authorService.GetByIdAsync(id);
            if (author == null)
                return NotFound();
             
            var vm = _mapper.Map<AuthorVM>(author);

            // 🔥 Load Identity user
            var user = await _userManager.FindByIdAsync(author.UserId);

            if (user != null)
            {
                vm.Email = user.Email;
            }
            return View(vm);
        }

        // 🔹 EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AuthorVM vm)
        {
            if (!ModelState.IsValid)
                ModelState.Remove("Password");
            if (!ModelState.IsValid)
                return View(vm);

            var author = await _authorService.GetByIdAsync(vm.Id);
            if (author == null)
                return NotFound();

            // 🔥 STEP 1: Update Identity User
            var user = await _userManager.FindByIdAsync(author.UserId);

            if (user == null)
                return BadRequest("Linked user not found");

            user.Email = vm.Email;
            user.UserName = vm.Email;
            user.FullName = vm.Name;

            var userUpdateResult = await _userManager.UpdateAsync(user);

            if (!userUpdateResult.Succeeded)
            {
                foreach (var error in userUpdateResult.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(vm);
            }

            // 🔥 STEP 2: Optional Password Change
            if (!string.IsNullOrWhiteSpace(vm.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var passResult = await _userManager.ResetPasswordAsync(user, token, vm.Password);

                if (!passResult.Succeeded)
                {
                    foreach (var error in passResult.Errors)
                        ModelState.AddModelError("", error.Description);

                    return View(vm);
                }
            }

            // 🔥 STEP 3: Update Author Profile
            var dto = _mapper.Map<AuthorDto>(vm);

            // 🔥 CRITICAL: preserve UserId
            dto.UserId = author.UserId;

            await _authorService.UpdateAsync(dto);

            // 🔥 STEP 4: Image handling
            if (vm.ImageFile != null)
            {
                if (!ImageValidator.IsValid(vm.ImageFile, out var error))
                {
                    ModelState.AddModelError("ImageFile", error);
                    return View(vm);
                }

                _imageService.DeleteAuthorImage(vm.Id, _env.WebRootPath);

                using var stream = vm.ImageFile.OpenReadStream();

                var imagePath = await _imageService.SaveAuthorImageAsync(
                    stream,
                    vm.Id,
                    _env.WebRootPath
                );

                await _authorService.UpdateImageAsync(vm.Id, imagePath);
            }

            return RedirectToAction(nameof(Index));
        }
        //public async Task<IActionResult> Edit(AuthorVM vm)
        //{
        //    if (!ModelState.IsValid)
        //        return View(vm);

        //    // Step 1: Update basic data
        //    var dto = _mapper.Map<AuthorDto>(vm);
        //    await _authorService.UpdateAsync(dto);

        //    // Step 2: Handle Image Update
        //    if (vm.ImageFile != null)
        //    {
        //        // ✅ Validate image
        //        if (!ImageValidator.IsValid(vm.ImageFile, out var error))
        //        {
        //            ModelState.AddModelError("ImageFile", error);
        //            return View(vm);
        //        }

        //        // Optional: delete old image first (clean replace)
        //        _imageService.DeleteAuthorImage(vm.Id, _env.WebRootPath);

        //        using var stream = vm.ImageFile.OpenReadStream();

        //        var imagePath = await _imageService.SaveAuthorImageAsync(
        //            stream,
        //            vm.Id,
        //            _env.WebRootPath
        //        );

        //        await _authorService.UpdateImageAsync(vm.Id, imagePath);
        //    }

        //    return RedirectToAction(nameof(Index));
        //}
        // 🔹 DELETE
        public async Task<IActionResult> Delete(int id)
        {
            await _authorService.DeleteAsync(id);

            // Delete image folder
            _imageService.DeleteAuthorImage(id, _env.WebRootPath);

            return RedirectToAction(nameof(Index));
        }
    }
}
