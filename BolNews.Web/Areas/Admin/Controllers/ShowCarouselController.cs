using BolNews.Domain.Common;
using BolNews.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class ShowCarouselController(ShowCarouselService carousel) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var chosen = await carousel.GetImagesAsync();
        var available = carousel.GetAvailableFiles();
        ViewBag.Available = chosen.Concat(available.Where(x => !chosen.Contains(x, StringComparer.OrdinalIgnoreCase))).ToList();
        ViewBag.Selected = chosen.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string[] selectedImages)
    {
        await carousel.SaveOrderAsync(selectedImages);
        TempData["Success"] = "Shows carousel updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile upload)
    {
        try
        {
            if (upload == null) throw new InvalidDataException("Choose an image to upload.");
            await carousel.SaveUploadAsync(upload);
            TempData["Success"] = "Image uploaded. Select it below and save the carousel order.";
        }
        catch (InvalidDataException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string name)
    {
        if (await carousel.DeleteAsync(name)) TempData["Success"] = "Image permanently deleted.";
        else TempData["Error"] = "Image could not be found or deleted.";
        return RedirectToAction(nameof(Index));
    }
}
