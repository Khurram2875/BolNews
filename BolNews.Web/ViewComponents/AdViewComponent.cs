using BolNews.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.ViewComponents
{
    public class AdViewComponent : ViewComponent
    {
        private readonly IAdService _adService;
        private readonly IWebHostEnvironment _env;

        public AdViewComponent(IAdService adService, IWebHostEnvironment env)
        {
            _adService = adService;
            _env = env;
        }

        public async Task<IViewComponentResult> InvokeAsync(string placementKey)
        {
            var ad = await _adService.GetPlacementAsync(placementKey);

            ViewBag.IsDevelopment = _env.IsDevelopment();

            return View(ad);
        }
    }
}
