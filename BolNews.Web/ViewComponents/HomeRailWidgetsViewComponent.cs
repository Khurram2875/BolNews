using BolNews.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.ViewComponents;

/// <summary>Poll and live events cards for one spot on the home page (for example "latest-top"). Renders nothing if neither is placed there.</summary>
public sealed class HomeRailWidgetsViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(HomeSlotsVM slots, string place)
    {
        ViewBag.Place = place;
        return View(slots);
    }
}
