using BolNews.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers;

[Route("site-theme.css")]
public sealed class SiteThemeController(SiteProfileService siteProfileService) : Controller
{
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var profile = await siteProfileService.GetAsync(cancellationToken);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Content(profile.CustomCss ?? string.Empty, "text/css; charset=utf-8");
    }
}
