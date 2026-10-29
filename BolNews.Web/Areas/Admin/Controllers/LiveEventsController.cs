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
        var vm = new LiveEventVM { Updates = [new LiveEventUpdateVM()], ElectionHeader = DefaultElectionHeader };
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
            EventType = IsElection(vm) ? LiveEventType.Election : LiveEventType.Standard,
            ElectionHeader = IsElection(vm) ? Clean(vm.ElectionHeader) ?? DefaultElectionHeader : null,
            RaceTitle = IsElection(vm) ? Clean(vm.RaceTitle) : null,
            RaceSubtitle = IsElection(vm) ? Clean(vm.RaceSubtitle) : null,
            Candidates = IsElection(vm) ? BuildCandidates(vm.Candidates) : new List<LiveEventCandidate>(),
            Updates = vm.Updates.Where(x => !string.IsNullOrWhiteSpace(x.Summary) || !string.IsNullOrWhiteSpace(x.Details))
                .Select(x => new LiveEventUpdate { Timestamp = AsUtc(x.Timestamp), Summary = x.Summary!.Trim(), Details = x.Details!.Trim(), Source = x.Source?.Trim(), UpdatedBy = name }).ToList(),
            LinkedArticles = vm.ArticleIds.Distinct().Select(id => new LiveEventArticle { ArticleId = id }).ToList()
        };
        db.LiveEvents.Add(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Live event created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.LiveEvents.Include(x => x.Updates).Include(x => x.LinkedArticles).Include(x => x.Candidates).FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        var vm = new LiveEventVM
        {
            Id = item.Id, Title = item.Title, Slug = item.Slug, FeaturedMediaId = item.FeaturedMediaId,
            IsActive = item.IsActive, IsPinned = item.IsPinned, ShowLiveBadge = item.ShowLiveBadge,
            EventType = item.EventType, ElectionHeader = item.ElectionHeader ?? DefaultElectionHeader,
            RaceTitle = item.RaceTitle, RaceSubtitle = item.RaceSubtitle,
            Candidates = item.Candidates.OrderBy(x => x.SortOrder)
                .Select(x => new LiveEventCandidateVM { Id = x.Id, Party = x.Party, Name = x.Name, Percentage = x.Percentage, Votes = x.Votes }).ToList(),
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
        var item = await db.LiveEvents.Include(x => x.Updates).Include(x => x.LinkedArticles).Include(x => x.Candidates).FirstOrDefaultAsync(x => x.Id == vm.Id);
        if (item == null) return NotFound();
        await ValidateAndPopulate(vm, vm.Id);
        if (!ModelState.IsValid) return View("Form", vm);
        if (vm.FeaturedImageUpload is { Length: > 0 })
            vm.FeaturedMediaId = await SaveFeaturedImageAsync(vm.FeaturedImageUpload);
        var name = User.Identity?.Name ?? users.GetUserId(User) ?? "Editorial";
        item.Title = vm.Title.Trim(); item.Slug = NormalizeSlug(vm.Slug); item.FeaturedMediaId = vm.FeaturedMediaId;
        item.IsActive = vm.IsActive; item.IsPinned = vm.IsPinned; item.ShowLiveBadge = vm.ShowLiveBadge;
        item.UpdatedAt = DateTime.UtcNow; item.UpdatedBy = name;
        SyncElectionCard(item, vm);
        db.LiveEventUpdates.RemoveRange(item.Updates);
        item.Updates = vm.Updates.Where(x => !string.IsNullOrWhiteSpace(x.Summary) || !string.IsNullOrWhiteSpace(x.Details))
            .Select(x => new LiveEventUpdate { LiveEventId = item.Id, Timestamp = AsUtc(x.Timestamp), Summary = x.Summary!.Trim(), Details = x.Details!.Trim(), Source = x.Source?.Trim(), UpdatedBy = name }).ToList();
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
    public async Task<IActionResult> UpdateResults(int id)
    {
        var item = await db.LiveEvents.AsNoTracking().Include(x => x.Candidates)
            .FirstOrDefaultAsync(x => x.Id == id && x.EventType == LiveEventType.Election);
        if (item == null) return NotFound();
        return View(ToResultsVM(item));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateResults(LiveEventResultsVM vm)
    {
        var item = await db.LiveEvents.Include(x => x.Candidates)
            .FirstOrDefaultAsync(x => x.Id == vm.Id && x.EventType == LiveEventType.Election);
        if (item == null) return NotFound();
        for (var i = 0; i < vm.Candidates.Count; i++) ValidateResultNumbers(vm.Candidates[i], $"Candidates[{i}]");
        if (!ModelState.IsValid)
        {
            // Names and parties are display-only on this screen, so restore them from the saved event.
            var display = ToResultsVM(item);
            foreach (var row in vm.Candidates)
            {
                var saved = display.Candidates.FirstOrDefault(x => x.Id == row.Id);
                if (saved == null) continue;
                row.Name = saved.Name; row.Party = saved.Party;
            }
            vm.Title = display.Title; vm.RaceTitle = display.RaceTitle; vm.RaceSubtitle = display.RaceSubtitle;
            return View(vm);
        }
        foreach (var row in vm.Candidates)
        {
            var candidate = item.Candidates.FirstOrDefault(x => x.Id == row.Id);
            if (candidate == null) continue;
            candidate.Percentage = Math.Round(row.Percentage, 2);
            candidate.Votes = row.Votes;
        }
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = User.Identity?.Name ?? users.GetUserId(User) ?? "Editorial";
        await db.SaveChangesAsync();
        TempData["Success"] = "Election results updated.";
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
        ValidateElection(vm);
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
        EnsureCandidateRows(vm);
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

    // ---- US election scoreboard card -------------------------------------------------
    private const string DefaultElectionHeader = "U.S. MIDTERM ELECTIONS";

    private static bool IsElection(LiveEventVM vm) => vm.EventType == LiveEventType.Election;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // The scoreboard card always has two rows (Republican, Democrat); pad or trim whatever was posted.
    private static void EnsureCandidateRows(LiveEventVM vm)
    {
        if (vm.Candidates.Count > 2) vm.Candidates = vm.Candidates.Take(2).ToList();
        while (vm.Candidates.Count < 2)
            vm.Candidates.Add(new LiveEventCandidateVM { Party = vm.Candidates.Count == 0 ? ElectionParty.Republican : ElectionParty.Democrat });
    }

    private void ValidateElection(LiveEventVM vm)
    {
        if (!IsElection(vm)) return;
        if (string.IsNullOrWhiteSpace(vm.RaceTitle))
            ModelState.AddModelError(nameof(vm.RaceTitle), "Enter the state or race name shown on the card.");
        if (string.IsNullOrWhiteSpace(vm.RaceSubtitle))
            ModelState.AddModelError(nameof(vm.RaceSubtitle), "Enter the office shown on the card, for example U.S. SENATE.");
        for (var i = 0; i < vm.Candidates.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(vm.Candidates[i].Name))
                ModelState.AddModelError($"Candidates[{i}].Name", "Enter the candidate name.");
            ValidateResultNumbers(vm.Candidates[i], $"Candidates[{i}]");
        }
    }

    private void ValidateResultNumbers(LiveEventCandidateVM row, string prefix)
    {
        if (row.Percentage < 0m || row.Percentage > 100m)
            ModelState.AddModelError($"{prefix}.Percentage", "Percentage must be between 0 and 100.");
        if (row.Votes < 0)
            ModelState.AddModelError($"{prefix}.Votes", "Votes cannot be negative.");
    }

    private static List<LiveEventCandidate> BuildCandidates(IEnumerable<LiveEventCandidateVM> rows) =>
        rows.Select((row, i) => new LiveEventCandidate
        {
            Party = row.Party, Name = row.Name!.Trim(), Percentage = Math.Round(row.Percentage, 2), Votes = row.Votes, SortOrder = i
        }).ToList();

    // Candidates are updated in place (matched by Id) so editing during live coverage never recreates rows.
    private void SyncElectionCard(LiveEvent item, LiveEventVM vm)
    {
        if (!IsElection(vm))
        {
            db.LiveEventCandidates.RemoveRange(item.Candidates.ToList());
            item.EventType = LiveEventType.Standard;
            item.ElectionHeader = null; item.RaceTitle = null; item.RaceSubtitle = null;
            return;
        }

        item.EventType = LiveEventType.Election;
        item.ElectionHeader = Clean(vm.ElectionHeader) ?? DefaultElectionHeader;
        item.RaceTitle = Clean(vm.RaceTitle);
        item.RaceSubtitle = Clean(vm.RaceSubtitle);

        var postedIds = vm.Candidates.Where(x => x.Id != 0).Select(x => x.Id).ToHashSet();
        db.LiveEventCandidates.RemoveRange(item.Candidates.Where(x => !postedIds.Contains(x.Id)).ToList());
        for (var i = 0; i < vm.Candidates.Count; i++)
        {
            var row = vm.Candidates[i];
            var existing = row.Id == 0 ? null : item.Candidates.FirstOrDefault(x => x.Id == row.Id);
            if (existing == null)
            {
                item.Candidates.Add(new LiveEventCandidate
                {
                    Party = row.Party, Name = row.Name!.Trim(), Percentage = Math.Round(row.Percentage, 2), Votes = row.Votes, SortOrder = i
                });
                continue;
            }
            existing.Party = row.Party; existing.Name = row.Name!.Trim();
            existing.Percentage = Math.Round(row.Percentage, 2); existing.Votes = row.Votes; existing.SortOrder = i;
        }
    }

    private static LiveEventResultsVM ToResultsVM(LiveEvent item) => new()
    {
        Id = item.Id, Title = item.Title, RaceTitle = item.RaceTitle, RaceSubtitle = item.RaceSubtitle,
        Candidates = item.Candidates.OrderBy(x => x.SortOrder)
            .Select(x => new LiveEventCandidateVM { Id = x.Id, Party = x.Party, Name = x.Name, Percentage = x.Percentage, Votes = x.Votes }).ToList()
    };
    // -----------------------------------------------------------------------------------

    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
    private static string NormalizeSlug(string value)
    {
        var slug = new string(value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
