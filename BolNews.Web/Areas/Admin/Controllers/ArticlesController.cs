using AutoMapper;
using BolNews.Application.Common;
using BolNews.Application.Common.Helpers;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Application.Services;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;
using BolNews.Infrastructure.Services;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
using BolNews.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class ArticlesController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly ICategoryService _categoryService;
        private readonly IAuthorService _authorService;
        private readonly IImageService _imageService;
        private readonly IWebHostEnvironment _env;
        private readonly IDiscoverService _discoverService;
        private readonly IHeadlineService _headlineService;
        private readonly ITrendingService _trendingService;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;

        public ArticlesController(
             IArticleService articleService,
             ICategoryService categoryService,
             IAuthorService authorService,
             IWebHostEnvironment env, IMapper mapper, IImageService imageService, IDiscoverService discoverService, IHeadlineService headlineService, ITrendingService trendingService, ICacheService cacheService, UserManager<ApplicationUser> userManager)
        {
            _articleService = articleService;
            _categoryService = categoryService;
            _authorService = authorService;
            _env = env;
            _mapper = mapper;
            _imageService = imageService;
            _discoverService = discoverService;
            _headlineService = headlineService;
            _trendingService = trendingService;
            _cacheService = cacheService;
            _userManager = userManager;
        }

        // GET: Admin/Articles
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var roles = await _userManager.GetRolesAsync(await _userManager.GetUserAsync(User));

            var dtos = await _articleService.GetAllAsync(userId, roles);
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
            //var authors = await _authorService.GetAllAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
            //ViewBag.Authors = new SelectList(authors, "Id", "Name", authorId);
        }
        // POST: Admin/Articles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ArticleVM model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model.CategoryId);
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
            var userId = _userManager.GetUserId(User);

            var author = await _authorService.GetAuthorByUserId(userId);
            //.FirstOrDefaultAsync(a => a.UserId == userId);

            if (author == null)
            {
                ModelState.AddModelError("", "Author profile not found.");
                return View(model);
            }

            var slug = await _articleService.GenerateUniqueSlugAsync(model.Title);

            var dto = _mapper.Map<ArticleDto>(model);
            dto.AuthorId = author.Id;

            dto.Slug = slug;
            dto.MetaTitle = model.MetaTitle ?? model.Title;
            dto.MetaDescription = model.MetaDescription;
            //await _articleService.CreateAsync(dto);
            //var articleId = await _articleService.CreateAsync(dto);
            var roles = await _userManager.GetRolesAsync(await _userManager.GetUserAsync(User));
            var articleId = await _articleService.CreateAsync(
                dto,
                userId,
                roles);

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
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            // 🔥 AUTHORIZATION CHECK
            var canEdit = await _articleService.CanEditAsync(id, user.Id, roles);

            if (!canEdit)
                return Forbid();

            var dto = await _articleService.GetByIdAsync(id);
            if (dto == null)
                return NotFound();

            var model = _mapper.Map<ArticleVM>(dto);

            model.SubmitForReview = dto.WorkflowStatus == ArticleWorkflowStatus.Submitted;
            // 🔥 show author name (read-only in UI)
            model.AuthorName = dto.AuthorName;

            model.DiscoverScore = _discoverService.Evaluate(new PublicArticleVM
            {
                Title = model.Title,
                MetaDescription = model.MetaDescription,
                Content = model.Content,
                FeaturedImageXl = model.FeaturedImageXl,
                PublishedAt = model.PublishedAt
            });

            await PopulateDropdowns(model.CategoryId);

            return View(model);
        }

        // POST: Admin/Articles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ArticleVM model)
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            // 🔥 AUTHORIZATION CHECK
            var canEdit = await _articleService.CanEditAsync(model.Id, user.Id, roles);

            if (!canEdit)
                return Forbid();

            if (!ModelState.IsValid)
            {
                model.DiscoverScore = _discoverService.Evaluate(new PublicArticleVM
                {
                    Title = model.Title,
                    MetaDescription = model.MetaDescription,
                    Content = model.Content,
                    FeaturedImageXl = model.FeaturedImageXl,
                    PublishedAt = model.PublishedAt
                });

                await PopulateDropdowns(model.CategoryId);
                return View(model);
            }

            var existing = await _articleService.GetByIdAsync(model.Id);
            if (existing == null)
                return NotFound();

            var oldSlug = existing.Slug;

            var dto = _mapper.Map<ArticleDto>(model);

            dto.FeaturedImageThumb = existing.FeaturedImageThumb;
            dto.FeaturedImageMedium = existing.FeaturedImageMedium;
            dto.FeaturedImageLarge = existing.FeaturedImageLarge;
            dto.FeaturedImageXl = existing.FeaturedImageXl;
            // 🔥 CRITICAL: PRESERVE AUTHOR
            dto.AuthorId = existing.AuthorId;

            // 🔥 ROLE-BASED PUBLISH CONTROL
            //if (!(roles.Contains("Admin") || roles.Contains("Editor")))
            //{
            //    dto.IsPublished = existing.IsPublished;
            //    dto.PublishedAt = existing.PublishedAt;
            //}

            // 🔹 Slug logic
            if (existing.Title != model.Title)
            {
                dto.Slug = await _articleService.GenerateUniqueSlugAsync(model.Title);
            }
            else
            {
                dto.Slug = existing.Slug;
            }

            await _articleService.UpdateAsync(dto, user.Id, roles, changeReason: "Article edited via CMS");

            // 🔹 Image handling (unchanged)
            if (model.ImageFile != null)
            {
                if (!ImageValidator.IsValid(model.ImageFile, out var error))
                {
                    ModelState.AddModelError("ImageFile", error);
                    await PopulateDropdowns(model.CategoryId);
                    return View(model);
                }

                _imageService.DeleteArticleImages(model.Id, _env.WebRootPath);

                using var stream = model.ImageFile.OpenReadStream();

                var (thumb, medium, large, xl) =
                    await _imageService.SaveArticleImagesAsync(stream, model.Id, _env.WebRootPath);

                await _articleService.UpdateImagesAsync(model.Id, thumb, medium, large, xl);
            }

            // 🔥 Cache invalidation (unchanged)
            _cacheService.Remove(CacheKeys.Article(oldSlug));
            _cacheService.Remove(CacheKeys.Article(dto.Slug));

            _cacheService.Remove($"article_content_{dto.Id}");
            _cacheService.Remove($"related_{dto.Id}");

            _cacheService.Remove(CacheKeys.Trending);
            _cacheService.Remove(CacheKeys.Dashboard);
            _cacheService.Remove(CacheKeys.Sitemap + "_index");
            _cacheService.Remove(CacheKeys.Sitemap + "_articles");
            _cacheService.Remove(CacheKeys.Sitemap + "_news");

            for (int i = 1; i <= 3; i++)
            {
                _cacheService.Remove(CacheKeys.Category(dto.CategorySlug, i));
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Articles/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            var canDelete = await _articleService.CanDeleteAsync(id, user.Id, roles);

            if (!canDelete)
                return Forbid();

            var article = await _articleService.GetByIdAsync(id);

            if (article == null)
                return NotFound();

            await _articleService.DeleteAsync(id);

            // 🔥 Cache invalidation
            _cacheService.Remove(CacheKeys.Article(article.Slug));
            _cacheService.Remove($"article_content_{article.Id}");
            _cacheService.Remove($"related_{article.Id}");

            _cacheService.Remove(CacheKeys.Trending);
            _cacheService.Remove(CacheKeys.Dashboard);

            for (int i = 1; i <= 3; i++)
            {
                _cacheService.Remove(CacheKeys.Category(article.CategorySlug, i));
            }

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

                // Fix #7: validate MIME type and file size before processing
                if (!ImageValidator.IsValid(upload, out var validationError))
                    return Json(new { error = new { message = validationError } });

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
        [HttpPost]
        public IActionResult GenerateHeadlines([FromBody] string title)
        {
            var result = _headlineService.Generate(title);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> TrendingTopics()
        {
            var topics = await _trendingService.GetTrendingTopicsAsync();
            return Json(topics);
        }
    }
}