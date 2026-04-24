using AutoMapper;
using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly ITrendingService _trendingService;
        private readonly IMapper _mapper;

        public DashboardController(
            IArticleService articleService,
            ITrendingService trendingService,
            IMapper mapper)
        {
            _articleService = articleService;
            _trendingService = trendingService;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardVM();

            // 🔢 Stats
            vm.TotalArticles = await _articleService.GetTotalArticlesAsync();
            vm.ArticlesToday = await _articleService.GetTodayArticlesCountAsync();

            // 🔥 Top Articles
            var top = await _articleService.GetTopArticlesAsync();
            vm.TopArticles = _mapper.Map<List<PublicArticleVM>>(top);

            // 🆕 Recent Articles
            var recent = await _articleService.GetRecentArticlesAsync(24);
            vm.RecentArticles = _mapper.Map<List<PublicArticleVM>>(recent);

            // 📈 Trending
            vm.TrendingTopics = await _trendingService.GetTrendingTopicsAsync();

            // ⚠ Low Performers
            var low = await _articleService.GetLowPerformingArticlesAsync();
            vm.LowPerformingArticles = _mapper.Map<List<PublicArticleVM>>(low);

            // 📈 Articles per day
            var stats = await _articleService.GetArticlesPerDayAsync(7);

            vm.Dates = stats.Select(x => x.date.ToString("MMM dd")).ToList();
            vm.ArticlesPerDay = stats.Select(x => x.count).ToList();

            // 🔥 Top articles chart
            vm.TopArticleTitles = vm.TopArticles.Select(a => a.Title).Take(5).ToList();
            vm.TopArticleViews = top.Take(5).Select(a => a.ViewCount).ToList();

            var categoryStats = await _articleService.GetCategoryPerformanceAsync(7);

            

            vm.CategoryPerformance = categoryStats;

            // For charts
            vm.CategoryNames = categoryStats.Select(c => c.CategoryName).ToList();
            vm.CategoryViews = categoryStats.Select(c => c.TotalViews).ToList();
            vm.CategoryAvgViews = categoryStats.Select(c => c.AvgViewsPerArticle).ToList();

            var editorStats = await _articleService.GetEditorPerformanceAsync(7);

            vm.EditorPerformance = editorStats;

            // For charts
            vm.EditorNames = editorStats.Select(e => e.AuthorName).ToList();
            vm.EditorViews = editorStats.Select(e => e.TotalViews).ToList();
            vm.EditorAvgViews = editorStats.Select(e => e.AvgViewsPerArticle).ToList();

            // 🏆 Top editor
            var topEditor = editorStats.FirstOrDefault();
            ViewBag.TopEditor = topEditor?.AuthorName;

            return View(vm);
        }
    }
}
