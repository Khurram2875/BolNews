using System.Globalization;
using System.Security.Cryptography;
using BolNews.Application.DTOs;
using BolNews.Application.Common.Helpers;
using BolNews.Application.Interfaces;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;
using BolNews.Persistence.Context;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin + "," + Roles.Editor + "," + Roles.SubEditor)]
public class PollsController(
    AppDbContext db,
    UserManager<ApplicationUser> users,
    IMediaLibraryService mediaLibrary,
    IImageService imageService,
    IWebHostEnvironment environment) : Controller
{
    private const int MaxOptions = 6;

    public async Task<IActionResult> Index()
    {
        var polls = await db.Polls.AsNoTracking().OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id, x.Title, x.PollType, x.IsActive, x.EndsAt,
                Image = x.FeaturedMedia.ThumbnailUrl ?? x.FeaturedMedia.Url,
                Options = x.Options.OrderBy(o => o.SortOrder).Select(o => new { o.Id, o.Text }).ToList()
            }).ToListAsync();
        var ids = polls.Select(p => p.Id).ToList();
        var counts = await db.PollVotes.AsNoTracking().Where(v => ids.Contains(v.PollId))
            .GroupBy(v => v.OptionId).Select(g => new { OptionId = g.Key, Count = g.Count() }).ToListAsync();
        var model = polls.Select(p =>
        {
            var options = p.Options.Select(o => (o.Text, counts.FirstOrDefault(c => c.OptionId == o.Id)?.Count ?? 0)).ToList();
            return new PollListItemVM
            {
                Id = p.Id, Title = p.Title, ImageUrl = p.Image, PollType = p.PollType, IsActive = p.IsActive,
                EndsAtUtc = DateTime.SpecifyKind(p.EndsAt, DateTimeKind.Utc), Options = options, TotalVotes = options.Sum(o => o.Item2)
            };
        }).ToList();
        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new PollVM { Options = [new PollOptionVM(), new PollOptionVM()] };
        await PopulateChoices(vm);
        return View("Form", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PollVM vm)
    {
        var endsAt = await ValidateAsync(vm, isNew: true);
        if (!ModelState.IsValid) { await PopulateChoices(vm); return View("Form", vm); }
        if (vm.FeaturedImageUpload is { Length: > 0 }) vm.FeaturedMediaId = await SaveFeaturedImageAsync(vm.FeaturedImageUpload);
        var name = User.Identity?.Name ?? users.GetUserId(User) ?? "Editorial";
        var poll = new Poll
        {
            Title = vm.Title.Trim(), Description = Clean(vm.Description), PollType = vm.PollType,
            FeaturedMediaId = vm.FeaturedMediaId, IsActive = vm.IsActive, EndsAt = endsAt,
            CreatedAt = DateTime.UtcNow, CreatedBy = name,
            Options = CleanOptions(vm).Select((o, i) => new PollOption { Text = o.Text!.Trim(), SortOrder = i }).ToList()
        };
        db.Polls.Add(poll);
        await db.SaveChangesAsync();
        if (poll.IsActive) await DeactivateOthersAsync(poll.Id);
        TempData["Success"] = "Poll created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var poll = await db.Polls.AsNoTracking().Include(x => x.Options).FirstOrDefaultAsync(x => x.Id == id);
        if (poll == null) return NotFound();
        var counts = await db.PollVotes.AsNoTracking().Where(v => v.PollId == id)
            .GroupBy(v => v.OptionId).Select(g => new { OptionId = g.Key, Count = g.Count() }).ToListAsync();
        var vm = new PollVM
        {
            Id = poll.Id, Title = poll.Title, Description = poll.Description, PollType = poll.PollType,
            FeaturedMediaId = poll.FeaturedMediaId, IsActive = poll.IsActive,
            EndsAtUtc = DateTime.SpecifyKind(poll.EndsAt, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ss'Z'"),
            Options = poll.Options.OrderBy(o => o.SortOrder)
                .Select(o => new PollOptionVM { Id = o.Id, Text = o.Text, Votes = counts.FirstOrDefault(c => c.OptionId == o.Id)?.Count ?? 0 }).ToList()
        };
        await PopulateChoices(vm);
        return View("Form", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PollVM vm)
    {
        var poll = await db.Polls.Include(x => x.Options).FirstOrDefaultAsync(x => x.Id == vm.Id);
        if (poll == null) return NotFound();
        var endsAt = await ValidateAsync(vm, isNew: false);
        if (!ModelState.IsValid)
        {
            // Keep the vote counts visible when the form is shown again.
            var counts = await db.PollVotes.AsNoTracking().Where(v => v.PollId == vm.Id)
                .GroupBy(v => v.OptionId).Select(g => new { OptionId = g.Key, Count = g.Count() }).ToListAsync();
            foreach (var o in vm.Options) o.Votes = counts.FirstOrDefault(c => c.OptionId == o.Id)?.Count ?? 0;
            await PopulateChoices(vm);
            return View("Form", vm);
        }
        if (vm.FeaturedImageUpload is { Length: > 0 }) vm.FeaturedMediaId = await SaveFeaturedImageAsync(vm.FeaturedImageUpload);
        poll.Title = vm.Title.Trim(); poll.Description = Clean(vm.Description); poll.PollType = vm.PollType;
        poll.FeaturedMediaId = vm.FeaturedMediaId; poll.IsActive = vm.IsActive; poll.EndsAt = endsAt;
        poll.UpdatedAt = DateTime.UtcNow; poll.UpdatedBy = User.Identity?.Name ?? users.GetUserId(User);

        // Options are updated in place (matched by Id) so editing a label never throws away its votes.
        var posted = CleanOptions(vm);
        var keepIds = posted.Where(o => o.Id != 0).Select(o => o.Id).ToHashSet();
        foreach (var removed in poll.Options.Where(o => !keepIds.Contains(o.Id)).ToList())
            db.PollOptions.Remove(removed);   // its votes are removed with it (cascade)
        for (var i = 0; i < posted.Count; i++)
        {
            var existing = posted[i].Id == 0 ? null : poll.Options.FirstOrDefault(o => o.Id == posted[i].Id);
            if (existing == null) poll.Options.Add(new PollOption { Text = posted[i].Text!.Trim(), SortOrder = i });
            else { existing.Text = posted[i].Text!.Trim(); existing.SortOrder = i; }
        }
        await db.SaveChangesAsync();
        if (poll.IsActive) await DeactivateOthersAsync(poll.Id);
        TempData["Success"] = "Poll updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var poll = await db.Polls.FirstOrDefaultAsync(x => x.Id == id);
        if (poll == null) return NotFound();
        poll.IsActive = !poll.IsActive;
        poll.UpdatedAt = DateTime.UtcNow; poll.UpdatedBy = User.Identity?.Name;
        await db.SaveChangesAsync();
        if (poll.IsActive) await DeactivateOthersAsync(poll.Id);
        TempData["Success"] = poll.IsActive
            ? "Poll is now active and shown on the site (any other active poll was switched off)."
            : "Poll switched off. With no active poll, the poll section disappears from the site.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetVotes(int id)
    {
        if (!await db.Polls.AnyAsync(x => x.Id == id)) return NotFound();
        await db.PollVotes.Where(v => v.PollId == id).ExecuteDeleteAsync();
        TempData["Success"] = "All votes for this poll were cleared.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var poll = await db.Polls.FirstOrDefaultAsync(x => x.Id == id);
        if (poll == null) return NotFound();
        db.Polls.Remove(poll);   // options and votes go with it
        await db.SaveChangesAsync();
        TempData["Success"] = "Poll deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ---- helpers ----------------------------------------------------------------------

    /// <summary>The site has one poll slot: switching a poll on switches every other poll off.</summary>
    private Task<int> DeactivateOthersAsync(int activeId) =>
        db.Polls.Where(x => x.Id != activeId && x.IsActive).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<PollOptionVM> CleanOptions(PollVM vm) => vm.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)).ToList();

    private async Task<DateTime> ValidateAsync(PollVM vm, bool isNew)
    {
        var endsAtUtc = default(DateTime);
        var hasUpload = vm.FeaturedImageUpload is { Length: > 0 };
        if (hasUpload && !ImageValidator.IsValid(vm.FeaturedImageUpload!, out var uploadError))
            ModelState.AddModelError(nameof(vm.FeaturedImageUpload), uploadError);
        if (!hasUpload && !await db.MediaAssets.AnyAsync(x => x.Id == vm.FeaturedMediaId && x.MediaType == MediaType.Image))
            ModelState.AddModelError(nameof(vm.FeaturedMediaId), "Choose an existing image or upload one: it becomes the card background.");

        if (!DateTimeOffset.TryParse(vm.EndsAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            ModelState.AddModelError(nameof(vm.EndsAtUtc), "Choose the voting end date and time.");
        else
        {
            endsAtUtc = parsed.UtcDateTime;
            if (isNew && endsAtUtc <= DateTime.UtcNow)
                ModelState.AddModelError(nameof(vm.EndsAtUtc), "The end time must be in the future.");
        }

        var options = CleanOptions(vm);
        if (vm.PollType is PollType.Versus or PollType.TrueFalse)
        {
            if (options.Count != 2) ModelState.AddModelError("Options", vm.PollType == PollType.Versus ? "A versus poll needs exactly two contenders." : "A true/false poll needs exactly two answers.");
        }
        else if (options.Count < 2 || options.Count > MaxOptions)
            ModelState.AddModelError("Options", $"Add between 2 and {MaxOptions} answers.");
        if (options.Select(o => o.Text!.Trim().ToLowerInvariant()).Distinct().Count() != options.Count)
            ModelState.AddModelError("Options", "Each answer must be different.");
        if (options.Any(o => o.Text!.Trim().Length > 150)) ModelState.AddModelError("Options", "An answer can be at most 150 characters.");
        return endsAtUtc;
    }

    private async Task PopulateChoices(PollVM vm)
    {
        vm.Images = await db.MediaAssets.AsNoTracking().Where(x => x.MediaType == MediaType.Image)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new SelectListItem((x.Caption ?? x.OriginalFileName ?? $"Image {x.Id}") + " — " + x.Url, x.Id.ToString(), x.Id == vm.FeaturedMediaId)).ToListAsync();
        while (vm.Options.Count < 2) vm.Options.Add(new PollOptionVM());
    }

    private async Task<int> SaveFeaturedImageAsync(Microsoft.AspNetCore.Http.IFormFile file)
    {
        // The hash includes a marker so a poll upload never reuses an older copy of the same file that the
        // Media Library already cropped to 16:9 (the poll card needs the uncropped picture).
        string hash;
        await using (var input = file.OpenReadStream())
        {
            using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[81920];
            int read;
            while ((read = await input.ReadAsync(buffer)) > 0) sha.AppendData(buffer, 0, read);
            sha.AppendData(System.Text.Encoding.UTF8.GetBytes("|uncropped"));
            hash = Convert.ToHexString(sha.GetHashAndReset());
        }
        var existing = await mediaLibrary.FindByContentHashAsync(hash, MediaType.Image);
        if (existing != null) return existing.Id;

        var userId = users.GetUserId(User)!;
        var dto = new MediaAssetDto
        {
            MediaType = MediaType.Image, Url = "/uploads/media/pending",
            OriginalFileName = Path.GetFileName(file.FileName), ContentHash = hash,
            AltText = Path.GetFileNameWithoutExtension(file.FileName)
        };
        var id = await mediaLibrary.CreateAsync(dto, userId);
        await using var imageStream = file.OpenReadStream();
        var (thumb, medium, large, xl) = await imageService.SaveMediaImagesUncroppedAsync(imageStream, id, environment.WebRootPath);
        await mediaLibrary.SetStorageAsync(id, xl, thumb, medium, large, userId);
        return id;
    }
}
