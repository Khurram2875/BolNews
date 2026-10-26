using BolNews.Application.Interfaces;
using BolNews.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Web.ViewComponents;

public class NavbarCategoriesViewComponent(
    ICategoryService categoryService,
    IMemoryCache cache,
    SiteProfileService siteProfiles) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await cache.GetOrCreateAsync("navbar_categories", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return await categoryService.GetParentCategoriesWithChildrenAsync();
        }) ?? [];

        var profile = await siteProfiles.GetAsync(HttpContext.RequestAborted);
        List<BolNews.Domain.Entities.Category> ordered;
        if (profile.NavigationCategoryIds is null)
        {
            var legacyOrder = new[] { "pakistan", "world", "sports", "entertainment", "business", "lifestyle", "technology" };
            ordered = legacyOrder.Select(slug => categories.FirstOrDefault(category =>
                    string.Equals(category.Slug, slug, StringComparison.OrdinalIgnoreCase)))
                .Where(category => category is not null)
                .Select(category => category!)
                .ToList();
        }
        else
        {
            var byId = categories
                .Where(category => !string.Equals(category.Slug, "latest-news", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(category => category.Id);
            var ids = profile.NavigationCategoryIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => int.TryParse(value, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .Take(9);
            ordered = ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        }

        return View(ordered);
    }
}
