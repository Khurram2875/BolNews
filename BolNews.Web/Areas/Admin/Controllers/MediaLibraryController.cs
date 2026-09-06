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
public class MediaLibraryController(
    IMediaLibraryService mediaLibrary,
    IImageService imageService,
    IWebHostEnvironment environment,
    UserManager<ApplicationUser> userManager,
    ICacheService cacheService) : Controller
{
    public async Task<IActionResult> Index(
        string? query,
        MediaType? type,
        DateTime? createdFrom,
        DateTime? createdTo,
        int? articleId)
    {
        ViewBag.Query = query;
        ViewBag.Type = type;
        ViewBag.CreatedFrom = createdFrom?.ToString("yyyy-MM-dd");
        ViewBag.CreatedTo = createdTo?.ToString("yyyy-MM-dd");

        return View(
            await mediaLibrary.SearchAsync(
                query,
                type,
                createdFrom,
                createdTo,
                articleId));
    }

    public async Task<IActionResult> Picker(
        string? query,
        MediaType? type)
        => Json(
            await mediaLibrary.SearchAsync(
                query,
                type,
                null,
                null,
                null));

    public IActionResult Upload()
        => View(
            new MediaAssetDto
            {
                MediaType = MediaType.Image
            });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(
        MediaAssetDto model,
        IFormFile upload,
        IFormFile? videoThumbnail)
    {
        if (upload == null || upload.Length == 0)
            ModelState.AddModelError(
                "upload",
                "Choose a media file.");

        if (model.MediaType == MediaType.Image &&
            upload != null &&
            !ImageValidator.IsValid(upload, out var error))
        {
            ModelState.AddModelError(
                "upload",
                error);
        }

        if (model.MediaType != MediaType.Image &&
            upload != null &&
            (upload.Length > 100 * 1024 * 1024 ||
             !IsMatchingMediaType(
                 model.MediaType,
                 upload.ContentType)))
        {
            ModelState.AddModelError(
                "upload",
                "The selected file does not match the media type or is too large.");
        }

        if (!ModelState.IsValid)
            return View(model);

        var userId = userManager.GetUserId(User)!;

        model.Url = "/uploads/media/pending";
        model.OriginalFileName = upload.FileName;

        var id = await mediaLibrary.CreateAsync(
            model,
            userId);

        await using var stream =
            upload.OpenReadStream();

        if (model.MediaType == MediaType.Image)
        {
            // Existing four-version image processing.
            var (thumb, medium, large, xl) =
                await imageService.SaveMediaImagesAsync(
                    stream,
                    id,
                    environment.WebRootPath);

            await mediaLibrary.SetStorageAsync(
                id,
                xl,
                thumb,
                medium,
                large,
                userId);
        }
        else if (model.MediaType == MediaType.Video)
        {
            // Save the original video.
            var videoUrl =
                await imageService.SaveMediaFileAsync(
                    stream,
                    upload.FileName,
                    id,
                    environment.WebRootPath);

            string? thumbnailUrl = null;

            // Save the browser-generated video thumbnail.
            if (videoThumbnail != null &&
                videoThumbnail.Length > 0)
            {
                if (!videoThumbnail.ContentType.Equals(
                    "image/jpeg",
                    StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        "videoThumbnail",
                        "Invalid video thumbnail.");

                    return View(model);
                }

                await using var thumbnailStream =
                    videoThumbnail.OpenReadStream();

                thumbnailUrl =
                    await imageService.SaveMediaFileAsync(
                        thumbnailStream,
                        $"video-{id}.jpg",
                        id,
                        environment.WebRootPath);
            }

            await mediaLibrary.SetStorageAsync(
                id,
                videoUrl,
                thumbnailUrl,
                null,
                null,
                userId);

            // A new video changes the public Videos page.
            cacheService.Remove("public_videos");
        }
        else
        {
            // Existing audio handling.
            var mediaUrl =
                await imageService.SaveMediaFileAsync(
                    stream,
                    upload.FileName,
                    id,
                    environment.WebRootPath);

            await mediaLibrary.SetStorageAsync(
                id,
                mediaUrl,
                null,
                null,
                null,
                userId);
        }

        return RedirectToAction(nameof(Index));
    }


    //Large File chunk upload handling for videos
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadVideoChunk(
    IFormFile chunk,
    string uploadId,
    int chunkIndex)
    {
        if (chunk == null || chunk.Length == 0)
            return BadRequest("Empty chunk.");

        if (string.IsNullOrWhiteSpace(uploadId))
            return BadRequest("Invalid upload ID.");

        if (chunkIndex < 0)
            return BadRequest("Invalid chunk index.");

        var tempRoot =
            Path.Combine(
                environment.WebRootPath,
                "uploads",
                "media",
                "temp",
                uploadId);

        Directory.CreateDirectory(tempRoot);

        var chunkPath =
            Path.Combine(
                tempRoot,
                $"chunk-{chunkIndex:D6}");

        await using var input =
            chunk.OpenReadStream();

        await using var output =
            new FileStream(
                chunkPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                1024 * 64,
                useAsync: true);

        await input.CopyToAsync(output);

        return Ok();
    }
    // Finalize the video upload after all chunks are uploaded (Completion)
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteVideoUpload(
    MediaAssetDto model,
    string uploadId,
    int totalChunks,
    string fileName,
    IFormFile? videoThumbnail)
    {
        if (string.IsNullOrWhiteSpace(uploadId))
            return BadRequest("Invalid upload ID.");

        if (totalChunks <= 0)
            return BadRequest("Invalid chunk count.");

        var userId =
            userManager.GetUserId(User)!;

        var tempRoot =
            Path.Combine(
                environment.WebRootPath,
                "uploads",
                "media",
                "temp",
                uploadId);

        if (!Directory.Exists(tempRoot))
            return BadRequest("Upload session not found.");

        var safeFileName =
            Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(safeFileName))
            safeFileName = "video.mp4";

        if (!safeFileName.EndsWith(
                ".mp4",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(
                "Only MP4 videos are supported.");
        }

        var assembledPath =
            Path.Combine(
                tempRoot,
                "assembled.mp4");

        try
        {
            // Assemble all chunks.
            await using (var output =
                new FileStream(
                    assembledPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    1024 * 64,
                    useAsync: true))
            {
                for (var i = 0; i < totalChunks; i++)
                {
                    var chunkPath =
                        Path.Combine(
                            tempRoot,
                            $"chunk-{i:D6}");

                    if (!System.IO.File.Exists(chunkPath))
                    {
                        return BadRequest(
                            $"Missing chunk {i}.");
                    }

                    await using (
                        var input = new FileStream(
                            chunkPath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.Read,
                            1024 * 64,
                            useAsync: true))
                    {
                        await input.CopyToAsync(output);
                    }
                }
            }

            // Create MediaAsset.
            model.MediaType = MediaType.Video;
            model.Url = "/uploads/media/pending";
            model.OriginalFileName = safeFileName;

            // Validate thumbnail before creating the MediaAsset
            if (videoThumbnail != null &&
                videoThumbnail.Length > 0)
            {
                if (!videoThumbnail.ContentType.Equals(
                        "image/jpeg",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(
                        "Invalid video thumbnail.");
                }
            }

            var id =
            await mediaLibrary.CreateAsync(
            model,
            userId);

            await using (var videoStream =
                new FileStream(
                    assembledPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    1024 * 64,
                    useAsync: true))
            {
                var videoUrl =
                    await imageService.SaveMediaFileAsync(
                        videoStream,
                        safeFileName,
                        id,
                        environment.WebRootPath);

                string? thumbnailUrl = null;

                if (videoThumbnail != null &&
                    videoThumbnail.Length > 0)
                {
                    await using var thumbnailStream =
                        videoThumbnail.OpenReadStream();

                    thumbnailUrl =
                        await imageService.SaveMediaFileAsync(
                            thumbnailStream,
                            $"video-{id}.jpg",
                            id,
                            environment.WebRootPath);
                }

                await mediaLibrary.SetStorageAsync(
                    id,
                    videoUrl,
                    thumbnailUrl,
                    null,
                    null,
                    userId);
            }

            // Remove temporary upload directory.
            try
            {
                Directory.Delete(
                    tempRoot,
                    recursive: true);
            }
            catch (IOException)
            {
                // Temporary upload cleanup failed.
                // The completed media is already safely stored.
            }
            catch (UnauthorizedAccessException)
            {
                // Temporary upload cleanup failed.
                // The completed media is already safely stored.
            }

            // Public Videos page changed.
            cacheService.Remove("public_videos");

            return Ok(new
            {
                success = true,
                mediaId = id
            });
        }
        catch
        {
            // Keep temporary files for now if something fails.
            // This makes troubleshooting easier.
            throw;
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model =
            await mediaLibrary.GetByIdAsync(id);

        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        MediaAssetDto model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await mediaLibrary.UpdateAsync(
            model,
            userManager.GetUserId(User)!);

        // Metadata such as caption/alt text may be displayed
        // on the public Videos page.
        if (model.MediaType == MediaType.Video)
        {
            cacheService.Remove("public_videos");
        }

        return RedirectToAction(nameof(Index));
    }

    private static bool IsMatchingMediaType(
        MediaType type,
        string? contentType) => type switch
        {
            MediaType.Audio =>
                contentType?.StartsWith(
                    "audio/",
                    StringComparison.OrdinalIgnoreCase) == true,

            MediaType.Video =>
                contentType?.StartsWith(
                    "video/",
                    StringComparison.OrdinalIgnoreCase) == true,

            _ => false
        };
}