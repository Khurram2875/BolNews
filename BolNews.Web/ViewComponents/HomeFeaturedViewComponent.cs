using BolNews.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.ViewComponents;

/// <summary>Home page Featured rail and Shows carousel.</summary>
public sealed class HomeFeaturedViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(HomeSlotsVM slots) => View(slots);
}
