using BolNews.Domain.Common;
using BolNews.Application.Common.Helpers;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Domain.Entities.Base;
using BolNews.Domain.Enums;
using BolNews.Persistence.Context;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace BolNews.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Editor + "," + Roles.SubEditor)]
public class LiveEventsController(
    AppDbContext db,
    UserManager<ApplicationUser> users,
    IMediaLibraryService mediaLibrary,
    IImageService imageService,
    IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Index()
    {
        var events = await db.LiveEvents.Include(x => x.FeaturedMedia).Include(x => x.Updates)
            .OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.CreatedAt).ToListAsync();
        return View(events);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new LiveEventVM { Updates = [new LiveEventUpdateVM()] };
        await PopulateChoices(vm);
        return View("Form", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LiveEventVM vm)
    {
        await ValidateAndPopulate(vm);
        if (!ModelState.IsValid) return View("Form", vm);
        if (vm.FeaturedImageUpload is { Length: > 0 })
            vm.FeaturedMediaId = await SaveFeaturedImageAsync(vm.FeaturedImageUpload);
        var name = User.Identity?.Name ?? users.GetUserId(User) ?? "Editorial";
        var item = new LiveEvent
        {
            Title = vm.Title.Trim(), Slug = NormalizeSlug(vm.Slug), FeaturedMediaId = vm.FeaturedMediaId,
            IsActive = vm.IsActive, IsPinned = vm.IsPinned, ShowLiveBadge = vm.ShowLiveBadge,
            CreatedAt = DateTime.UtcNow, CreatedBy = name,
            Updates = vm.Updates.Where(x => !string.IsNullOrWhiteSpace(x.Summary) || !string.IsNullOrWhiteSpace(x.Details))
                .Select(x => new LiveEventUpdate { Timestamp = AsUtc(x.Timestamp), Summary = x.Summary.Trim(), Details = x.Details.Trim(), Source = x.Source?.Trim(), UpdatedBy = name }).ToList(),
            LinkedArticles = vm.ArticleIds.Distinct().Select(id => new LiveEventArticle { ArticleId = id }).ToList()
        };
        db.LiveEvents.Add(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Live event created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.LiveEvents.Include(x => x.Updates).Include(x => x.LinkedArticles).FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        var vm = new LiveEventVM
        {
            Id = item.Id, Title = item.Title, Slug = item.Slug, FeaturedMediaId = item.FeaturedMediaId,
            IsActive = item.IsActive, IsPinned = item.IsPinned, ShowLiveBadge = item.ShowLiveBadge,
            ArticleIds = item.LinkedArticles.Select(x => x.ArticleId).ToList(),
            Updates = item.Updates.OrderBy(x => x.Timestamp).Select(x => new LiveEventUpdateVM { Id = x.Id, Timestamp = x.Timestamp, Summary = x.Summary, Details = x.Details, Source = x.Source }).ToList()
        };
        if (vm.Updates.Count == 0) vm.Updates.Add(new LiveEventUpdateVM());
        await PopulateChoices(vm);
        return View("Form", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LiveEventVM vm)
    {
        var item = await db.LiveEvents.Include(x => x.Updates).Include(x => x.LinkedArticles).FirstOrDefaultAsync(x => x.Id == vm.Id);
        if (item == null) return NotFound();
        await ValidateAndPopulate(vm, vm.Id);
        if (!ModelState.IsValid) return View("Form", vm);
        if (vm.FeaturedImageUpload is { Length: > 0 })
            vm.FeaturedMediaId = await SaveFeaturedImageAsync(vm.FeaturedImageUpload);
        var name = User.Identity?.Name ?? users.GetUserId(User) ?? "Editorial";
        item.Title = vm.Title.Trim(); item.Slug = NormalizeSlug(vm.Slug); item.FeaturedMediaId = vm.FeaturedMediaId;
        item.IsActive = vm.IsActive; item.IsPinned = vm.IsPinned; item.ShowLiveBadge = vm.ShowLiveBadge;
        item.UpdatedAt = DateTime.UtcNow; item.UpdatedBy = name;
        db.LiveEventUpdates.RemoveRange(item.Updates);
        item.Updates = vm.Updates.Where(x => !string.IsNullOrWhiteSpace(x.Summary) || !string.IsNullOrWhiteSpace(x.Details))
            .Select(x => new LiveEventUpdate { LiveEventId = item.Id, Timestamp = AsUtc(x.Timestamp), Summary = x.Summary.Trim(), Details = x.Details.Trim(), Source = x.Source?.Trim(), UpdatedBy = name }).ToList();
        db.LiveEventArticles.RemoveRange(item.LinkedArticles);
        item.LinkedArticles = vm.ArticleIds.Distinct().Select(id => new LiveEventArticle { LiveEventId = item.Id, ArticleId = id }).ToList();
        await db.SaveChangesAsync();
        TempData["Success"] = "Live event updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.LiveEvents.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        db.LiveEvents.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Live event deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> SearchPublishedArticles(string? term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
            return Json(Array.Empty<object>());

        var query = term.Trim();
        var articles = await db.Articles.AsNoTracking()
            .Where(x => x.IsPublished && (x.Title.Contains(query) || x.Summary.Contains(query)))
            .OrderByDescending(x => x.PublishedAt).Take(20)
            .Select(x => new { id = x.Id, title = x.Title, category = x.Category.Name })
            .ToListAsync();
        return Json(articles);
    }

    private async Task ValidateAndPopulate(LiveEventVM vm, int? currentId = null)
    {
        await PopulateChoices(vm);
        var hasNewUpload = vm.FeaturedImageUpload is { Length: > 0 };
        if (hasNewUpload && !ImageValidator.IsValid(vm.FeaturedImageUpload!, out var uploadError))
            ModelState.AddModelError(nameof(vm.FeaturedImageUpload), uploadError);
        if (string.IsNullOrWhiteSpace(vm.Slug)) vm.Slug = NormalizeSlug(vm.Title);
        vm.Slug = NormalizeSlug(vm.Slug);
        if (await db.LiveEvents.AnyAsync(x => x.Id != currentId && x.Slug == vm.Slug)) ModelState.AddModelError(nameof(vm.Slug), "This slug is already in use.");
        if (!hasNewUpload && !await db.MediaAssets.AnyAsync(x => x.Id == vm.FeaturedMediaId && x.MediaType == MediaType.Image))
            ModelState.AddModelError(nameof(vm.FeaturedMediaId), "Choose an existing image or upload a featured image.");
        var validIds = await db.Articles.Where(x => vm.ArticleIds.Contains(x.Id) && x.IsPublished).Select(x => x.Id).ToListAsync();
        if (validIds.Count != vm.ArticleIds.Distinct().Count()) ModelState.AddModelError(nameof(vm.ArticleIds), "One or more selected articles are unavailable.");
        for (var i = 0; i < vm.Updates.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(vm.Updates[i].Summary) && string.IsNullOrWhiteSpace(vm.Updates[i].Details)) continue;
            if (string.IsNullOrWhiteSpace(vm.Updates[i].Summary)) ModelState.AddModelError($"Updates[{i}].Summary", "Enter an update summary.");
            if (string.IsNullOrWhiteSpace(vm.Updates[i].Details)) ModelState.AddModelError($"Updates[{i}].Details", "Enter update details.");
        }
    }

    private async Task PopulateChoices(LiveEventVM vm)
    {
        vm.Images = await db.MediaAssets.AsNoTracking().Where(x => x.MediaType == MediaType.Image)
            .OrderByDescending(x => x.CreatedAt).Select(x => new SelectListItem((x.Caption ?? x.OriginalFileName ?? $"Image {x.Id}") + " — " + x.Url, x.Id.ToString(), x.Id == vm.FeaturedMediaId)).ToListAsync();
        vm.SelectedArticles = await db.Articles.AsNoTracking().Where(x => x.IsPublished && vm.ArticleIds.Contains(x.Id))
            .OrderByDescending(x => x.PublishedAt)
            .Select(x => new LiveEventArticleOptionVM { Id = x.Id, Title = x.Title, Category = x.Category.Name })
            .ToListAsync();
        vm.ArticleIds = vm.SelectedArticles.Select(x => x.Id).ToList();
    }

    private async Task<int> SaveFeaturedImageAsync(Microsoft.AspNetCore.Http.IFormFile file)
    {
        string hash;
        await using (var input = file.OpenReadStream())
            hash = Convert.ToHexString(await SHA256.HashDataAsync(input));

        var existing = await mediaLibrary.FindByContentHashAsync(hash, MediaType.Image);
        if (existing != null) return existing.Id;

        var userId = users.GetUserId(User)!;
        var dto = new MediaAssetDto
        {
            MediaType = MediaType.Image,
            Url = "/uploads/media/pending",
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentHash = hash,
            AltText = Path.GetFileNameWithoutExtension(file.FileName)
        };
        var id = await mediaLibrary.CreateAsync(dto, userId);
        await using var imageStream = file.OpenReadStream();
        var (thumb, medium, large, xl) = await imageService.SaveMediaImagesAsync(imageStream, id, environment.WebRootPath);
        await mediaLibrary.SetStorageAsync(id, xl, thumb, medium, large, userId);
        return id;
    }

    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
    private static string NormalizeSlug(string value)
    {
        var slug = new string(value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
