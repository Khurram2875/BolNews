using System.Text.RegularExpressions;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class SiteSettingsController(SiteProfileService profiles) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.Themes = new[] { "default", "dark", "editorial", "bold" };
        return View(await profiles.GetAsync(cancellationToken));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SiteProfile model, CancellationToken cancellationToken)
    {
        ViewBag.Themes = new[] { "default", "dark", "editorial", "bold" };

        if (string.IsNullOrWhiteSpace(model.SiteName) || string.IsNullOrWhiteSpace(model.ShortName))
            ModelState.AddModelError(nameof(model.SiteName), "Enter the publisher name and short name.");
        if (!IsColor(model.PrimaryColor) || !IsColor(model.AccentColor))
            ModelState.AddModelError(nameof(model.PrimaryColor), "Brand colors must use the #RRGGBB format.");
        if (!new[] { "default", "dark", "editorial", "bold" }.Contains(model.ThemeName, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(model.ThemeName), "Choose a supported theme.");
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
        await profiles.SaveAsync(model, cancellationToken);
        TempData["SuccessMessage"] = "Publisher settings saved. Changes are visible immediately.";
        return RedirectToAction(nameof(Index));
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
