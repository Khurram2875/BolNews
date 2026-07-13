using AutoMapper;
using Azure;
using BolNews.Application.Common;
using BolNews.Application.Common.Helpers;
using BolNews.Application.DTOs;
using BolNews.Application.DTOs.Grammar;
using BolNews.Application.Interfaces;
using BolNews.Application.Services;
using BolNews.Domain.Common;
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
using System.Text.Json;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Authorize(Roles =
    Roles.Admin + "," +
    Roles.Editor + "," +
    Roles.SubEditor + "," +
    Roles.Author + "," +
        Roles.Factchecker)]
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
        private readonly IArticleLockService _articleLockService;
        private readonly IArticleDiscussionService _discussionService;
        private readonly ICacheService _cacheService;
        private readonly IEditorialPlacementService _editorialPlacementService;
        private readonly ITagService _tagService;
        private readonly IGeminiService _geminiService;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGrammarService _grammarService;
        private readonly IArticleRevisionService _articleRevisionService;
        private readonly IArticleDiffService _articleDiffService;

        public ArticlesController(
             IArticleService articleService,
             ICategoryService categoryService,
             IAuthorService authorService,
             IWebHostEnvironment env, IMapper mapper, IImageService imageService,
             IDiscoverService discoverService, IHeadlineService headlineService,
             ITrendingService trendingService, ICacheService cacheService,
             UserManager<ApplicationUser> userManager, IArticleLockService articleLockService,
             IArticleDiscussionService discussionService, IGeminiService geminiService,
             IEditorialPlacementService editorialPlacementService,
             ITagService tagService,
             IGrammarService grammarService,
             IArticleRevisionService articleRevisionService,
             IArticleDiffService articleDiffService)
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
            _articleLockService = articleLockService;
            _discussionService = discussionService;
            _geminiService = geminiService;
            _editorialPlacementService = editorialPlacementService;
            _tagService = tagService;
            _grammarService = grammarService;
            _articleRevisionService = articleRevisionService;
            _articleDiffService = articleDiffService;
        }

        // GET: Admin/Articles
        public async Task<IActionResult> Index(int page = 1)
        {
            //var userId = _userManager.GetUserId(User);
            //var roles = await _userManager.GetRolesAsync(await _userManager.GetUserAsync(User));

            //var dtos = await _articleService.GetAllAsync(userId, roles);
            //var viewModels = _mapper.Map<List<ArticleVM>>(dtos);
            //return View(viewModels);

            if (page < 1) page = 1;

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

            // 1. Fetch all matching business objects/DTOs for this user's permission layer
            var dtos = await _articleService.GetAllAsync(userId, roles);

            // 2. Setup pagination layout parameters
            const int pageSize = 10; // Number of records displayed per page
            int totalRecords = dtos.Count();

            // 3. Slice the data server-side
            var pagedDtos = dtos
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 4. Map only the sliced subset to your ViewModels array
            var viewModels = _mapper.Map<List<ArticleVM>>(pagedDtos);

            // 5. Populate tracking metrics into ViewBag flags
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < ViewBag.TotalPages;

            if (roles.Contains(Roles.Admin) || roles.Contains(Roles.Editor))
            {
                var topStory = await _editorialPlacementService.GetPinnedTopStoryAsync();
                var secondaryStories = await _editorialPlacementService.GetPinnedSecondaryStoriesAsync();

                ViewBag.PinnedTopStoryArticleId = topStory?.ArticleId;
                ViewBag.SecondaryPlacementByArticleId = secondaryStories
                    .ToDictionary(x => x.ArticleId, x => x.Id);
            }

            return View(viewModels);
        }

        public async Task<IActionResult> Revisions(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var roles = await _userManager.GetRolesAsync(user);

            if (!await CanViewArticleRevisionsAsync(id, user.Id, roles))
                return Forbid();

            var article = await _articleService.GetByIdAsync(id);

            if (article == null)
                return NotFound();

            var revisions =
                await _articleRevisionService.GetByArticleIdAsync(id);

            var userNames =
                await GetUserDisplayNamesAsync(
                    revisions.Select(x => x.ChangedByUserId));

            var model =
                new ArticleRevisionHistoryVM
                {
                    Article = _mapper.Map<ArticleVM>(article),
                    Revisions = revisions
                        .Select(x => new ArticleRevisionListItemVM
                        {
                            Revision = x,
                            ChangedByName = GetDisplayName(
                                userNames,
                                x.ChangedByUserId)
                        })
                        .ToList()
                };

            return View(model);
        }

        public async Task<IActionResult> RevisionPreview(
            int articleId,
            int revisionId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var roles = await _userManager.GetRolesAsync(user);

            if (!await CanViewArticleRevisionsAsync(articleId, user.Id, roles))
                return Forbid();

            var article = await _articleService.GetByIdAsync(articleId);
            var revision = await _articleRevisionService.GetByIdAsync(revisionId);

            if (article == null || revision == null || revision.ArticleId != articleId)
                return NotFound();

            var userNames =
                await GetUserDisplayNamesAsync(
                    new[] { revision.ChangedByUserId });

            var model =
                new ArticleRevisionPreviewVM
                {
                    Article = _mapper.Map<ArticleVM>(article),
                    Revision = revision,
                    ChangedByName = GetDisplayName(
                        userNames,
                        revision.ChangedByUserId)
                };

            return View(model);
        }

        public async Task<IActionResult> CompareRevision(
            int articleId,
            int revisionId,
            string target = "current")
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var roles = await _userManager.GetRolesAsync(user);

            if (!await CanViewArticleRevisionsAsync(articleId, user.Id, roles))
                return Forbid();

            var article = await _articleService.GetByIdAsync(articleId);
            var revision = await _articleRevisionService.GetByIdAsync(revisionId);

            if (article == null || revision == null || revision.ArticleId != articleId)
                return NotFound();

            var comparison =
                target.Equals("previous", StringComparison.OrdinalIgnoreCase)
                    ? await CompareWithPreviousRevisionAsync(articleId, revision)
                    : _articleDiffService.Compare(
                        revision,
                        article,
                        "Current Article");

            if (comparison == null)
            {
                TempData["Error"] =
                    "There is no earlier revision to compare with.";

                return RedirectToAction(
                    nameof(Revisions),
                    new { id = articleId });
            }

            var userNames =
                await GetUserDisplayNamesAsync(
                    new[] { revision.ChangedByUserId });

            var model =
                new ArticleRevisionCompareVM
                {
                    Article = _mapper.Map<ArticleVM>(article),
                    Comparison = comparison,
                    FromChangedByName = GetDisplayName(
                        userNames,
                        revision.ChangedByUserId)
                };

            return View(model);
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
            var tags = await _tagService.GetAllAsync();
            //var authors = await _authorService.GetAllAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
            ViewBag.TagSuggestions = tags.Select(x => x.Name).ToList();
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
                .Select(x => new
                {
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

            if (user == null)
                return Challenge();

            var roles = await _userManager.GetRolesAsync(user);

            // Authorization
            var canEdit = await _articleService.CanEditAsync(id, user.Id, roles);

            if (!canEdit)
                return Forbid();

            // Load actual entity for locking
            var articleEntity = await _articleService.GetEntityByIdAsync(id);

            if (articleEntity == null)
                return NotFound();

            // Try acquire lock
            var acquired = await _articleLockService.TryAcquireLockAsync(
                articleEntity,
                user.Id);

            if (!acquired)
            {
                var lockOwner = await _articleLockService.GetLockOwnerNameAsync(articleEntity, user.Id);

                TempData["Error"] =
                    $"This article is currently being edited by {lockOwner}.";

                return RedirectToAction(nameof(Index));
            }

            var dto = await _articleService.GetByIdAsync(id);

            if (dto == null)
                return NotFound();

            var model = _mapper.Map<ArticleVM>(dto);
            model.ArticleTagsInput = JsonSerializer.Serialize(model.ArticleTags.Select(x => x.Name));
            model.FeaturedImageTagsInput = JsonSerializer.Serialize(model.FeaturedImageTags.Select(x => x.Name));

            model.DiscussionComments = await _discussionService.GetThreadAsync(id, user.Id, roles);

            model.SubmitForReview = dto.WorkflowStatus == ArticleWorkflowStatus.Submitted;

            model.AuthorName = dto.AuthorName;

            model.DiscoverScore = _discoverService.Evaluate(
                new PublicArticleVM
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

            if (user == null)
                return Challenge();

            var roles = await _userManager.GetRolesAsync(user);

            // Authorization
            var canEdit = await _articleService.CanEditAsync(
                model.Id,
                user.Id,
                roles);

            if (!canEdit)
                return Forbid();

            // Load entity for lock validation
            var articleEntity = await _articleService.GetEntityByIdAsync(model.Id);

            if (articleEntity == null)
                return NotFound();

            // Block if another user owns active lock
            if (_articleLockService.IsLockedByAnotherUser(articleEntity, user.Id))
            {
                TempData["Error"] =
                    "This article is currently locked by another newsroom user.";

                return RedirectToAction(nameof(Index));
            }

            // Refresh lock ownership while editing
            await _articleLockService.TryAcquireLockAsync(
                articleEntity,
                user.Id);

            if (!ModelState.IsValid)
            {
                model.DiscoverScore = _discoverService.Evaluate(
                    new PublicArticleVM
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

            // Preserve existing images unless replaced
            dto.FeaturedImageThumb = existing.FeaturedImageThumb;
            dto.FeaturedImageMedium = existing.FeaturedImageMedium;
            dto.FeaturedImageLarge = existing.FeaturedImageLarge;
            dto.FeaturedImageXl = existing.FeaturedImageXl;


            // Preserve author ownership
            dto.AuthorId = existing.AuthorId;

            // Slug logic
            dto.Slug = existing.Title != model.Title
                ? await _articleService.GenerateUniqueSlugAsync(model.Title)
                : existing.Slug;

            await _articleService.UpdateAsync(
                dto,
                user.Id,
                roles,
                changeReason: "Article edited via CMS");

            // Image replacement
            if (model.ImageFile != null)
            {
                if (!ImageValidator.IsValid(model.ImageFile, out var error))
                {
                    ModelState.AddModelError("ImageFile", error);

                    await PopulateDropdowns(model.CategoryId);

                    return View(model);
                }

                _imageService.DeleteArticleImages(
                    model.Id,
                    _env.WebRootPath);

                using var stream = model.ImageFile.OpenReadStream();

                var (thumb, medium, large, xl) =
                    await _imageService.SaveArticleImagesAsync(
                        stream,
                        model.Id,
                        _env.WebRootPath);

                await _articleService.UpdateImagesAsync(
                    model.Id,
                    thumb,
                    medium,
                    large,
                    xl);
            }

            // Release lock after successful save
            await _articleLockService.ReleaseLockAsync(
                articleEntity,
                user.Id);

            // Cache invalidation
            _cacheService.Remove(CacheKeys.Article(oldSlug));
            _cacheService.Remove(CacheKeys.Article(dto.Slug));

            _cacheService.Remove($"article_content_{dto.Id}");
            _cacheService.Remove($"related_{dto.Id}");

            _cacheService.Remove(CacheKeys.Trending("today"));

            _cacheService.Remove(CacheKeys.Trending("week"));

            _cacheService.Remove(CacheKeys.Trending("month"));
            _cacheService.Remove(CacheKeys.HomePage);

            _cacheService.Remove(CacheKeys.Dashboard);
            _cacheService.Remove(CacheKeys.Sitemap + "_index");
            _cacheService.Remove(CacheKeys.Sitemap + "_articles");
            _cacheService.Remove(CacheKeys.Sitemap + "_news");
            _cacheService.Remove(CacheKeys.Sitemap + "_tags");

            for (int i = 1; i <= 5; i++)
            {
                _cacheService.Remove(
                    CacheKeys.Category(dto.CategorySlug, i));
            }

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        //[IgnoreAntiforgeryToken]
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshLock(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var article =
                await _articleService.GetEntityByIdAsync(id);

            if (article == null)
                return NotFound();

            await _articleLockService.RefreshLockAsync(article, user.Id);

            return Ok();
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseLock(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var article =
                await _articleService.GetEntityByIdAsync(id);

            if (article == null)
                return NotFound();

            await _articleLockService.ReleaseLockAsync(article, user.Id);

            return Ok();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDiscussionComment(int articleId, string message)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var roles = await _userManager.GetRolesAsync(user);

            await _discussionService.AddCommentAsync(articleId, message, user.Id, roles);

            //var canEdit = await _articleService.CanEditAsync(articleId, user.Id, roles);

            //if (canEdit)
            //{
            //    return RedirectToAction(nameof(Discussion), new { id = articleId });
            //}

            return RedirectToAction(nameof(Discussion), new { id = articleId });
        }
        public async Task<IActionResult> Discussion(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var roles = await _userManager.GetRolesAsync(user);

            var article = await _articleService.GetEntityByIdAsync(id);

            if (article == null)
                return NotFound();

            var comments = await _discussionService.GetThreadAsync(
                id,
                user.Id,
                roles);

            var vm = new ArticleDiscussionVM
            {
                ArticleId = article.Id,
                Title = article.Title,
                AuthorName = article.Author?.Name ?? "Unknown",
                Thumbnail = article.FeaturedImageThumb,
                WorkflowStatus = article.WorkflowStatus,
                DiscussionComments = comments
            };

            return View(vm);
        }
        //Generate MetaTitle and Meta Description through Gemini
        [HttpPost]
        [Route("admin/articles/generatemetadata")]
        public async Task<IActionResult> GenerateMetadata([FromBody] MetadataRequestDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Title))
            {
                return BadRequest("Invalid data provided.");
            }

            // Call your Gemini/AI Service here using request.Title, request.Summary, request.Category
            // Example:
            // var aiResult = await _geminiService.GenerateMetaTagsAsync(request.Title, request.Summary, request.Category);

            // Await execution from the service wrapper
            var (metaTitle, metaDescription) = await _geminiService.GenerateMetadataAsync(
                request.Title,
                request.Summary,
                request.Category
            );

            // Return in the exact JSON signature our Javascript expects: { metaTitle: "...", metaDescription: "..." }
            return Json(new
            {
                metaTitle = metaTitle,
                metaDescription = metaDescription
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckGrammar([FromBody] GrammarCheckRequest request, CancellationToken cancellationToken)
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest(new
                {
                    message = "Article content is required."
                });
            }

            var issues = await _grammarService.CheckAsync(
                request.Text,
                request.Language,
                cancellationToken);

            return Ok(new
            {
                issues
            });
        }

        private async Task<bool> CanViewArticleRevisionsAsync(
            int articleId,
            string userId,
            IList<string> roles)
        {
            if (!(
                roles.Contains(Roles.Admin) ||
                roles.Contains(Roles.Editor) ||
                roles.Contains(Roles.SubEditor)))
            {
                return false;
            }

            return await _articleService.CanEditAsync(
                articleId,
                userId,
                roles);
        }

        private async Task<ArticleRevisionCompareDto?> CompareWithPreviousRevisionAsync(
            int articleId,
            ArticleRevisionDto revision)
        {
            var revisions =
                await _articleRevisionService.GetByArticleIdAsync(articleId);

            var previousRevision =
                revisions
                    .Where(x => x.RevisionNumber < revision.RevisionNumber)
                    .OrderByDescending(x => x.RevisionNumber)
                    .FirstOrDefault();

            if (previousRevision == null)
                return null;

            return _articleDiffService.Compare(
                previousRevision,
                revision,
                $"Revision {revision.RevisionNumber}");
        }

        private async Task<Dictionary<string, string>> GetUserDisplayNamesAsync(
            IEnumerable<string> userIds)
        {
            var ids =
                userIds
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

            if (ids.Count == 0)
                return new Dictionary<string, string>();

            return await _userManager.Users
                .Where(x => ids.Contains(x.Id))
                .ToDictionaryAsync(
                    x => x.Id,
                    x => !string.IsNullOrWhiteSpace(x.FullName)
                        ? x.FullName
                        : x.UserName ?? x.Email ?? x.Id);
        }

        private static string GetDisplayName(
            Dictionary<string, string> userNames,
            string userId)
        {
            return userNames.TryGetValue(userId, out var name)
                ? name
                : userId;
        }

        public class MetadataRequestDto
        {
            public string Title { get; set; }
            public string Summary { get; set; }
            public string Category { get; set; }
        }
        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            var articles = await _articleService.SearchArticlesAsync(term);

            var result = articles.Select(a => new
            {
                id = a.Id,
                text = a.Title
            });

            return Json(result);
        }
    }
}
