using BolNews.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.ViewComponents;

/// <summary>Home page category columns, in the publisher's chosen order.</summary>
public sealed class HomeCategoriesViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(HomeSlotsVM slots) => View(slots);
}
