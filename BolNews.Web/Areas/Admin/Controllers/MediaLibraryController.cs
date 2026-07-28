using BolNews.Application.Common.Helpers;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Common;
using BolNews.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BolNews.Domain.Entities;

namespace BolNews.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin + "," + Roles.Editor + "," + Roles.SubEditor + "," + Roles.Author)]
public class MediaLibraryController(IMediaLibraryService mediaLibrary, IImageService imageService, IWebHostEnvironment environment, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(string? query, MediaType? type, DateTime? createdFrom, DateTime? createdTo, int? articleId)
    {
        ViewBag.Query = query; ViewBag.Type = type; ViewBag.CreatedFrom = createdFrom?.ToString("yyyy-MM-dd"); ViewBag.CreatedTo = createdTo?.ToString("yyyy-MM-dd");
        return View(await mediaLibrary.SearchAsync(query, type, createdFrom, createdTo, articleId));
    }

    public async Task<IActionResult> Picker(string? query, MediaType? type)
        => Json(await mediaLibrary.SearchAsync(query, type, null, null, null));

    public IActionResult Upload() => View(new MediaAssetDto { MediaType = MediaType.Image });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(MediaAssetDto model, IFormFile upload)
    {
        if (upload == null || upload.Length == 0) ModelState.AddModelError("upload", "Choose a media file.");
        if (model.MediaType == MediaType.Image && upload != null && !ImageValidator.IsValid(upload, out var error)) ModelState.AddModelError("upload", error);
        if (model.MediaType != MediaType.Image && upload != null && (upload.Length > 100 * 1024 * 1024 || !IsMatchingMediaType(model.MediaType, upload.ContentType))) ModelState.AddModelError("upload", "The selected file does not match the media type or is too large.");
        if (!ModelState.IsValid) return View(model);
        var userId = userManager.GetUserId(User)!;
        model.Url = "/uploads/media/pending"; model.OriginalFileName = upload.FileName;
        var id = await mediaLibrary.CreateAsync(model, userId);
        await using var stream = upload.OpenReadStream();
        if (model.MediaType == MediaType.Image)
        {
            var (thumb, medium, large, xl) = await imageService.SaveMediaImagesAsync(stream, id, environment.WebRootPath);
            await mediaLibrary.SetStorageAsync(id, xl, thumb, medium, large, userId);
        }
        else await mediaLibrary.SetStorageAsync(id, await imageService.SaveMediaFileAsync(stream, upload.FileName, id, environment.WebRootPath), null, null, null, userId);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await mediaLibrary.GetByIdAsync(id); if (model == null) return NotFound(); return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MediaAssetDto model)
    {
        if (!ModelState.IsValid) return View(model);
        await mediaLibrary.UpdateAsync(model, userManager.GetUserId(User)!);
        return RedirectToAction(nameof(Index));
    }

    private static bool IsMatchingMediaType(MediaType type, string? contentType) => type switch
    {
        MediaType.Audio => contentType?.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) == true,
        MediaType.Video => contentType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true,
        _ => false
    };
}
