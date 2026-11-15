using BolNews.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.ViewComponents;

/// <summary>Home page left/right rail: polls, live events and the Latest list. Stories come pre-decided in HomeSlotsVM.</summary>
public sealed class HomeLatestViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(HomeSlotsVM slots) => View(slots);
}
