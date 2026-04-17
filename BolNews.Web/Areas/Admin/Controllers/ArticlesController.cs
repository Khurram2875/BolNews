using AutoMapper;
using BolNews.Application.Common.Helpers;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Application.Services;
using BolNews.Infrastructure.Services;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ArticlesController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly ICategoryService _categoryService;
        private readonly IAuthorService _authorService;
        private readonly IImageService _imageService;
        private readonly IWebHostEnvironment _env;

        private readonly IMapper _mapper;

        public ArticlesController(
             IArticleService articleService,
             ICategoryService categoryService,
             IAuthorService authorService,
             IWebHostEnvironment env, IMapper mapper, IImageService imageService)
        {
            _articleService = articleService;
            _categoryService = categoryService;
            _authorService = authorService;
            _env = env;
            _mapper = mapper;
            _imageService = imageService;
        }

        // GET: Admin/Articles
        public async Task<IActionResult> Index()
        {
            var dtos = await _articleService.GetAllAsync();
            var viewModels = _mapper.Map<List<ArticleVM>>(dtos);
            return View(viewModels);
        }

        // GET: Admin/Articles/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new ArticleVM());
        }
        private async Task PopulateDropdowns(int? categoryId = null, int? authorId = null)
        {
            var categories = await _categoryService.GetAllAsync();
            var authors = await _authorService.GetAllAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
            ViewBag.Authors = new SelectList(authors, "Id", "Name", authorId);
        }
        // POST: Admin/Articles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ArticleVM model)
        {

            if (!ModelState.IsValid)
            {
                var errorList = ModelState.Where(x => x.Value.Errors.Count > 0)
                .Select(x => new {
                    Property = x.Key,
                    Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                }).ToList();

                foreach (var error in errorList)
                {
                    // Print to the Output window in Visual Studio
                    Console.WriteLine($"Property: {error.Property}, Error: {string.Join(", ", error.Errors)}");
                }
                return View(model);
            }
                
            var slug = await _articleService.GenerateUniqueSlugAsync(model.Title);
            
            var dto = _mapper.Map<ArticleDto>(model);

            dto.Slug = slug;
            dto.MetaTitle = model.MetaTitle ?? model.Title;
            dto.MetaDescription = model.MetaDescription;
            //await _articleService.CreateAsync(dto);
            var articleId = await _articleService.CreateAsync(dto);
            if (model.ImageFile != null)
            {
                if (!ImageValidator.IsValid(model.ImageFile, out var error))
                {
                    ModelState.AddModelError("ImageFile", error);
                    return View(model);
                }

                using var stream = model.ImageFile.OpenReadStream();

                var (thumb, medium, large, xl) =
                    await _imageService.SaveArticleImagesAsync(stream, articleId, _env.WebRootPath);

                await _articleService.UpdateImagesAsync(articleId, thumb, medium, large, xl);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Articles/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _articleService.GetByIdAsync(id);
            if (dto == null)
                return NotFound();

            var model = _mapper.Map<ArticleVM>(dto);
            await PopulateDropdowns();
            return View(model);
        }

        // POST: Admin/Articles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ArticleVM model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var existing = await _articleService.GetByIdAsync(model.Id);
            var dto = _mapper.Map<ArticleDto>(model);
            if (existing.Title != model.Title)
            {
                dto.Slug = await _articleService.GenerateUniqueSlugAsync(model.Title);
            }
            else
            {
                dto.Slug = existing.Slug;
            }
                await _articleService.UpdateAsync(dto);

            if (model.ImageFile != null)
            {
                if (!ImageValidator.IsValid(model.ImageFile, out var error))
                {
                    ModelState.AddModelError("ImageFile", error);
                    return View(model);
                }

                _imageService.DeleteArticleImages(model.Id, _env.WebRootPath);

                using var stream = model.ImageFile.OpenReadStream();

                var (thumb, medium, large, xl) =
                    await _imageService.SaveArticleImagesAsync(stream, model.Id, _env.WebRootPath);

                await _articleService.UpdateImagesAsync(model.Id, thumb, medium, large, xl);
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Articles/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _articleService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // Optional: Details view
        public async Task<IActionResult> Details(int id)
        {
            var dto = await _articleService.GetByIdAsync(id);
            if (dto == null)
                return NotFound();

            var model = _mapper.Map<ArticleVM>(dto);
            return View(model);
        }
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadEditorImage(IFormFile upload)
        {
            try
            {
                if (upload == null || upload.Length == 0)
                    return Json(new { error = new { message = "No file uploaded" } });

                var folderPath = Path.Combine(_env.WebRootPath, "uploads", "articles", "content");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var fileName = $"{Guid.NewGuid()}.webp";
                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = upload.OpenReadStream())
                using (var image = await Image.LoadAsync(stream))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(500, 0),
                        Mode = ResizeMode.Max
                    }));
                    await image.SaveAsync(filePath, new WebpEncoder { Quality = 75 });
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var url = $"{baseUrl}/uploads/articles/content/{fileName}";

                return Json(new { url = url });
            }
            catch (Exception ex)
            {
                return Json(new { error = new { message = ex.Message } });
            }
        }
    }
}