using BolNews.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.ViewComponents
{
    public class SmartTrendingViewComponent : ViewComponent
    {
        private readonly IArticleService _articleService;

        public SmartTrendingViewComponent(IArticleService articleService)
        {
            _articleService = articleService;
        }

        public async Task<IViewComponentResult> InvokeAsync(int count = 5)
        {
            var articles =
                await _articleService.GetTopRankedPublishedAsync(count);

            return View(articles);
        }
    }
}
