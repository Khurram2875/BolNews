using BolNews.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.ViewComponents;

/// <summary>Home page centre panel: top story, secondary cards and trending.</summary>
public sealed class HomeTopStoriesViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(HomeSlotsVM slots) => View(slots);
}
