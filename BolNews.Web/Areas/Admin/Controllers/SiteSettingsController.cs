using System.Text.RegularExpressions;
using BolNews.Application.Common.Helpers;
using BolNews.Application.Interfaces;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;

namespace BolNews.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class SiteSettingsController(SiteProfileService profiles, IWebHostEnvironment environment, ICategoryService categories) : Controller
{
    private static readonly HashSet<string> BrandImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await profiles.GetAsync(cancellationToken);
        await PopulateOptionsAsync(model.NavigationCategoryIds, cancellationToken);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(3_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 3_000_000)]
    public async Task<IActionResult> UploadBrandAsset(IFormFile? upload, string? target, CancellationToken cancellationToken)
    {
        if (target is not ("HeaderLogoUrl" or "FooterLogoUrl" or "FaviconUrl"))
            return BadRequest(new { error = "Choose a supported publisher image field." });
        if (upload is null || upload.Length == 0)
            return BadRequest(new { error = "Choose an image file to upload." });

        var extension = Path.GetExtension(upload.FileName);
        var isIco = target == "FaviconUrl" && extension.Equals(".ico", StringComparison.OrdinalIgnoreCase);
        if (!BrandImageExtensions.Contains(extension) && !isIco)
            return BadRequest(new { error = target == "FaviconUrl" ? "Upload a JPG, PNG, WEBP, or ICO image." : "Upload a JPG, PNG, or WEBP image." });

        if (upload.Length > 2 * 1024 * 1024)
            return BadRequest(new { error = "The image must be 2 MB or smaller." });

        if (isIco)
        {
            await using var stream = upload.OpenReadStream();
            var header = new byte[6];
            var read = await stream.ReadAsync(header, cancellationToken);
            if (read != header.Length || header[0] != 0 || header[1] != 0 || header[2] != 1 || header[3] != 0 || header[4] == 0 || header[5] == 0)
                return BadRequest(new { error = "The selected ICO file is invalid." });
        }
        else if (!ImageValidator.IsValid(upload, out var validationError))
        {
            return BadRequest(new { error = validationError });
        }
        else
        {
            try
            {
                await using var imageStream = upload.OpenReadStream();
                using var image = await Image.LoadAsync(imageStream, cancellationToken);
                if (image.Width > 6000 || image.Height > 6000)
                    return BadRequest(new { error = "Image dimensions must be 6000 pixels or smaller." });
            }
            catch (UnknownImageFormatException)
            {
                return BadRequest(new { error = "The selected file is not a supported image." });
            }
            catch (InvalidImageContentException)
            {
                return BadRequest(new { error = "The selected image file is damaged or invalid." });
            }
        }

        var safeExtension = extension.ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var uploadDirectory = Path.Combine(environment.WebRootPath, "uploads", "brand");
        Directory.CreateDirectory(uploadDirectory);
        var destination = Path.GetFullPath(Path.Combine(uploadDirectory, fileName));
        var expectedDirectory = Path.GetFullPath(uploadDirectory) + Path.DirectorySeparatorChar;
        if (!destination.StartsWith(expectedDirectory, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Invalid file name." });

        await using (var output = System.IO.File.Create(destination))
            await upload.CopyToAsync(output, cancellationToken);

        return Ok(new { path = $"/uploads/brand/{fileName}" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SiteProfile model, List<int?> navigationCategoryOrder, CancellationToken cancellationToken)
    {
        var availableCategories = await categories.GetParentCategoriesWithChildrenAsync();
        var selectableCategories = availableCategories
            .Where(category => !string.Equals(category.Slug, "latest-news", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var validCategoryIds = selectableCategories.Select(x => x.Id).ToHashSet();
        var selectedCategoryIds = navigationCategoryOrder
            .Where(id => id.HasValue && validCategoryIds.Contains(id.Value))
            .Select(id => id!.Value)
            .Distinct()
            .Take(9)
            .ToList();
        model.NavigationCategoryIds = string.Join(',', selectedCategoryIds);
        await PopulateOptionsAsync(model.NavigationCategoryIds, cancellationToken, selectedCategoryIds);

        if (string.IsNullOrWhiteSpace(model.SiteName) || string.IsNullOrWhiteSpace(model.ShortName))
            ModelState.AddModelError(nameof(model.SiteName), "Enter the publisher name and short name.");
        if (!IsColor(model.PrimaryColor) || !IsColor(model.AccentColor))
            ModelState.AddModelError(nameof(model.PrimaryColor), "Brand colors must use the #RRGGBB format.");
        if (!PublisherThemeCatalog.ThemeNames.Contains(model.ThemeName, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(model.ThemeName), "Choose a supported theme.");
        if (!PublisherThemeCatalog.FontNames.Contains(model.FontFamily, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(model.FontFamily), "Choose a supported font family.");
        if (!IsSafeUrlOrPath(model.HeaderLogoUrl) || !IsSafeUrlOrPath(model.FooterLogoUrl) || !IsSafeUrlOrPath(model.FaviconUrl) || !IsSafeUrlOrPath(model.LiveStreamUrl) || !IsSafeUrlOrPath(model.LiveVideoEmbedUrl) || !IsSafeUrlOrPath(model.LiveThumbnailUrl))
            ModelState.AddModelError(string.Empty, "Logo, favicon, thumbnail, and live links must be local paths or HTTPS URLs.");
        if (!string.IsNullOrWhiteSpace(model.LiveVideoEmbedUrl) && !IsHttpsUrl(model.LiveVideoEmbedUrl))
            ModelState.AddModelError(nameof(model.LiveVideoEmbedUrl), "The live player embed URL must use HTTPS.");
        if (!string.IsNullOrWhiteSpace(model.UrduEditionUrl) && !IsHttpsUrl(model.UrduEditionUrl))
            ModelState.AddModelError(nameof(model.UrduEditionUrl), "Other edition links must be HTTPS URLs.");
        if (!IsValidFooterLinks(model.FooterColumnOneLinks) || !IsValidFooterLinks(model.FooterColumnTwoLinks) || !IsValidFooterLinks(model.FooterColumnThreeLinks))
            ModelState.AddModelError(string.Empty, "Footer links must use one Label|/local-path or Label|https://external-url per line.");
        if (!string.IsNullOrWhiteSpace(model.AdScriptUrl) && !IsHttpsUrl(model.AdScriptUrl))
            ModelState.AddModelError(nameof(model.AdScriptUrl), "The ad provider script must be an HTTPS URL.");
        if (model.CustomCss?.Length > 100_000)
            ModelState.AddModelError(nameof(model.CustomCss), "Custom CSS must be 100,000 characters or fewer.");
        foreach (var (field, url) in new[]
        {
            (nameof(model.WhatsAppUrl), model.WhatsAppUrl), (nameof(model.FacebookUrl), model.FacebookUrl),
            (nameof(model.XUrl), model.XUrl), (nameof(model.YouTubeUrl), model.YouTubeUrl),
            (nameof(model.InstagramUrl), model.InstagramUrl), (nameof(model.TikTokUrl), model.TikTokUrl)
        })
        {
            if (!string.IsNullOrWhiteSpace(url) && !IsHttpsUrl(url))
                ModelState.AddModelError(field, "Social links must be HTTPS URLs.");
        }

        if (!ModelState.IsValid)
            return View(model);

        model.Id = 1;
        model.ThemeName = PublisherThemeCatalog.NormalizeTheme(model.ThemeName);
        model.FontFamily = PublisherThemeCatalog.NormalizeFont(model.FontFamily);
        await profiles.SaveAsync(model, cancellationToken);
        TempData["SuccessMessage"] = "Publisher settings saved. Changes are visible immediately.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(string? navigationCategoryIds, CancellationToken cancellationToken, IReadOnlyCollection<int>? postedIds = null)
    {
        ViewBag.Themes = PublisherThemeCatalog.ThemeNames;
        ViewBag.FontFamilies = PublisherThemeCatalog.FontNames;
        var allCategories = await categories.GetParentCategoriesWithChildrenAsync();
        ViewBag.NavigationCategories = allCategories
            .Where(category => !string.Equals(category.Slug, "latest-news", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (postedIds is not null)
        {
            ViewBag.NavigationCategoryOrder = postedIds;
            return;
        }

        var ids = string.IsNullOrWhiteSpace(navigationCategoryIds)
            ? Array.Empty<int>()
            : navigationCategoryIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => int.TryParse(value, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToArray();
        if (navigationCategoryIds is null)
        {
            var legacySlugs = new[] { "pakistan", "world", "sports", "entertainment", "business", "lifestyle", "technology" };
            ids = legacySlugs.Select(slug => allCategories.FirstOrDefault(x => string.Equals(x.Slug, slug, StringComparison.OrdinalIgnoreCase))?.Id ?? 0)
                .Where(id => id > 0).ToArray();
        }
        var validIds = allCategories.Select(x => x.Id).ToHashSet();
        ViewBag.NavigationCategoryOrder = ids.Where(validIds.Contains).Distinct().Take(9).ToArray();
    }

    private static bool IsColor(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$");

    private static bool IsSafeUrlOrPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (value.StartsWith('/') || value.StartsWith("~/", StringComparison.Ordinal)) return true;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    }

    private static bool IsHttpsUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;

    private static bool IsValidFooterLinks(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        foreach (var line in value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split('|', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1])) return false;
            var target = parts[1];
            if (target.StartsWith('/') && !target.StartsWith("//", StringComparison.Ordinal)) continue;
            if (!IsHttpsUrl(target)) return false;
        }
        return true;
    }
}
