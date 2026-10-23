using BolNews.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.ViewComponents;

public sealed class AdZoneViewComponent(AdPlacementService placements) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string zone)
    {
        if (!AdPlacementZones.Contains(zone))
            return Content(string.Empty);

        ViewBag.IsInlineZone = AdPlacementZones.IsInline(zone);
        return View(await placements.GetEnabledForZoneAsync(zone, HttpContext.RequestAborted));
    }
}
