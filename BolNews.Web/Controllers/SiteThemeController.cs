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
        // Never serve CSS that is switched off or that fails the safety check (for example data saved before the check existed).
        var css = profile.CustomCss ?? string.Empty;
        if (!profile.Style.CustomCssEnabled || CustomCssValidator.Validate(css).Count > 0)
            css = string.Empty;
        return Content(css, "text/css; charset=utf-8");
    }
}
