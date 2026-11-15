using BolNews.Web.Helpers;
using BolNews.Web.Services;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace BolNews.Web.ViewComponents;

/// <summary>Renders one publisher HTML block slot. Content is sanitised again here, so stored HTML is never trusted.</summary>
public sealed class CustomBlockViewComponent(SiteProfileService profiles) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string slot)
    {
        var profile = await profiles.GetAsync(HttpContext.RequestAborted);
        var html = LiveHtml.Sanitize(profile.Blocks.Get(slot));
        if (!LiveHtml.HasContent(html))
            return new HtmlContentViewComponentResult(HtmlString.Empty);
        var css = "bn-custom-block bn-custom-block--" + slot.ToLowerInvariant();
        return new HtmlContentViewComponentResult(new HtmlString($"<div class=\"{css}\" data-custom-block=\"{slot}\">{html}</div>"));
    }
}
