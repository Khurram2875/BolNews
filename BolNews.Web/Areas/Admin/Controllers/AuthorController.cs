using AutoMapper;
using BolNews.Application.Common.Helpers;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public AuthorController(
            IAuthorService authorService,
            IImageService imageService,
            IMapper mapper,
            IWebHostEnvironment env)
        {
            _authorService = authorService;
            _imageService = imageService;
            _mapper = mapper;
            _env = env;
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
            return View();
        }

        // 🔹 CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AuthorVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            // Step 1: Create Author
            var dto = _mapper.Map<AuthorDto>(vm);
            var authorId = await _authorService.CreateAsync(dto);

            // Step 2: Save Image (if provided)
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
                    authorId.Id,
                    _env.WebRootPath
                );

                await _authorService.UpdateImageAsync(authorId.Id, imagePath);
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
            return View(vm);
        }

        // 🔹 EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AuthorVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            // Step 1: Update basic data
            var dto = _mapper.Map<AuthorDto>(vm);
            await _authorService.UpdateAsync(dto);

            // Step 2: Handle Image Update
            if (vm.ImageFile != null)
            {
                // ✅ Validate image
                if (!ImageValidator.IsValid(vm.ImageFile, out var error))
                {
                    ModelState.AddModelError("ImageFile", error);
                    return View(vm);
                }

                // Optional: delete old image first (clean replace)
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
