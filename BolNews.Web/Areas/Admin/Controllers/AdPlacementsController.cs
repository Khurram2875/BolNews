using System.Text.RegularExpressions;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdPlacementsController(AdPlacementService placements) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new AdPlacementsIndexVM
        {
            Placements = await placements.GetAllAsync(cancellationToken),
            NewPlacement = new AdPlacement
            {
                ZoneKey = AdPlacementZones.All[0].Key,
                ContainerClass = "",
                InsertionMode = "zone"
            }
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Add([Bind(Prefix = "NewPlacement")] AdPlacement model, CancellationToken cancellationToken)
        => SavePlacementAsync(model, cancellationToken);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Save(AdPlacement model, CancellationToken cancellationToken)
        => SavePlacementAsync(model, cancellationToken);

    private async Task<IActionResult> SavePlacementAsync(AdPlacement model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Key) || !Regex.IsMatch(model.Key, "^[a-zA-Z0-9._-]{2,100}$"))
            ModelState.AddModelError(nameof(model.Key), "Use 2–100 letters, numbers, dots, underscores, or hyphens for the key.");
        if (string.IsNullOrWhiteSpace(model.DisplayName))
            ModelState.AddModelError(nameof(model.DisplayName), "Enter a display name.");
        if (string.IsNullOrWhiteSpace(model.ElementId) || !Regex.IsMatch(model.ElementId, "^[A-Za-z][A-Za-z0-9_:-]{0,127}$"))
            ModelState.AddModelError(nameof(model.ElementId), "The div ID must start with a letter and contain only letters, numbers, _, :, or -.");
        if (!AdPlacementZones.Contains(model.ZoneKey))
            ModelState.AddModelError(nameof(model.ZoneKey), "Choose a supported page zone.");
        if (model.ParagraphIndex is < 1 or > 100)
            ModelState.AddModelError(nameof(model.ParagraphIndex), "Paragraph position must be between 1 and 100.");
        if (!string.IsNullOrWhiteSpace(model.ContainerClass) && !Regex.IsMatch(model.ContainerClass, "^[A-Za-z0-9 _-]{0,300}$"))
            ModelState.AddModelError(nameof(model.ContainerClass), "Container classes can contain letters, numbers, spaces, hyphens, and underscores.");
        if (!string.IsNullOrWhiteSpace(model.TargetSelector) && (model.TargetSelector.Length > 500 || Regex.IsMatch(model.TargetSelector, @"[<>;{}]")))
            ModelState.AddModelError(nameof(model.TargetSelector), "Enter a CSS selector up to 500 characters without markup or statement separators.");
        if (!new[] { "beforebegin", "afterbegin", "beforeend", "afterend" }.Contains(model.TargetPosition, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(model.TargetPosition), "Choose a supported target position.");

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await placements.SaveAsync(model, cancellationToken);
            TempData["SuccessMessage"] = "Ad placement saved and applied immediately.";
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            TempData["ErrorMessage"] = "The placement could not be saved. Check that the key is unique and the selected zone is supported.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await placements.DeleteAsync(id, cancellationToken);
        TempData["SuccessMessage"] = "Ad placement deleted.";
        return RedirectToAction(nameof(Index));
    }
}
