using BolNews.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Web.ViewComponents
{
    public class NavbarCategoriesViewComponent : ViewComponent
    {
        private readonly ICategoryService _categoryService;
        private readonly IMemoryCache _cache;

        public NavbarCategoriesViewComponent(
            ICategoryService categoryService,
            IMemoryCache cache)
        {
            _categoryService = categoryService;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _cache.GetOrCreateAsync("navbar_categories", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

                return await _categoryService.GetParentCategoriesWithChildrenAsync();
            });

            return View(categories);
        }
    }
}
