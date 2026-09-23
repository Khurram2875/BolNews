using AutoMapper;
using BolNews.Application.Interfaces;
using BolNews.Application.Interfaces.Scoring;
using BolNews.Domain.Common;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Authorize(Roles =
    Roles.Admin + "," +
    Roles.Editor + "," +
    Roles.SubEditor)]
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly ITrendingService _trendingService;
        private readonly IMapper _mapper;
        private readonly IAnalyticsService _analyticsService;
        private readonly IArticleScoringService _articleScoringService;

        public DashboardController(IArticleService articleService, ITrendingService trendingService,IMapper mapper, IAnalyticsService analyticsService, IArticleScoringService articleScoringService)
        {
            _articleService = articleService;
            _trendingService = trendingService;
            _mapper = mapper;
            _analyticsService = analyticsService;
            _articleScoringService = articleScoringService;
        }

        public async Task<IActionResult> Index(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var today = DateTime.UtcNow.Date;
            var selectedToDate = (toDate ?? today).Date;
            var selectedFromDate = (fromDate ?? selectedToDate.AddDays(-6)).Date;

            if (selectedFromDate > selectedToDate)
            {
                (selectedFromDate, selectedToDate) = (selectedToDate, selectedFromDate);
            }

            var vm = new DashboardVM
            {
                FromDate = selectedFromDate,
                ToDate = selectedToDate
            };

            // 🔢 Stats
            vm.TotalArticles = await _articleService.GetTotalArticlesAsync(selectedFromDate, selectedToDate);
            vm.TotalPublishedArticles = await _articleService.GetTotalPublishedArticlesAsync();
            vm.ArticlesToday = await _articleService.GetTodayArticlesCountAsync();

            // 🔥 Top Articles
            var top = await _articleService.GetTopArticlesAsync(selectedFromDate, selectedToDate, 10);
            vm.TopArticles = _mapper.Map<List<PublicArticleVM>>(top);

            // 🆕 Recent Articles
            var recent = await _articleService.GetRecentArticlesAsync(selectedFromDate, selectedToDate, 200);
            vm.RecentArticles = _mapper.Map<List<PublicArticleVM>>(recent);

            // 📈 Trending
            vm.TrendingTopics = await _trendingService.GetTrendingTopicsAsync(selectedFromDate, selectedToDate);

            // ⚠ Low Performers
            var low = await _articleService.GetLowPerformingArticlesAsync(selectedFromDate, selectedToDate);
            vm.LowPerformingArticles = _mapper.Map<List<PublicArticleVM>>(low);

            // 📈 Articles per day
            var stats = await _articleService.GetArticlesPerDayAsync(selectedFromDate, selectedToDate);

            vm.Dates = stats.Select(x => x.date.ToString("MMM dd")).ToList();
            vm.ArticlesPerDay = stats.Select(x => x.count).ToList();

            // 🔥 Top articles chart
            vm.TopArticleTitles = vm.TopArticles.Select(a => a.Title).Take(5).ToList();
            vm.TopArticleViews = top.Take(5).Select(a => a.ViewCount).ToList();
            vm.TopArticleUrls = top.Take(5)
                .Select(a => a.Category != null && !string.IsNullOrWhiteSpace(a.Category.Slug) && !string.IsNullOrWhiteSpace(a.Slug)
                    ? $"/news/{a.Category.Slug}/{a.Slug}"
                    : string.Empty)
                .ToList();

            var categoryStats = await _articleService.GetCategoryPerformanceAsync(selectedFromDate, selectedToDate);

            var lowCtrArticles = await _analyticsService.GetLowCTRArticlesAsync(selectedFromDate, selectedToDate);
            ViewBag.LowCTRArticles = lowCtrArticles;

            vm.CategoryPerformance = categoryStats;

            // For charts
            vm.CategoryNames = categoryStats.Select(c => c.CategoryName).ToList();
            vm.CategoryViews = categoryStats.Select(c => c.TotalViews).ToList();
            vm.CategoryAvgViews = categoryStats.Select(c => c.AvgViewsPerArticle).ToList();

            var editorStats = await _articleService.GetEditorPerformanceAsync(selectedFromDate, selectedToDate);

            vm.EditorPerformance = editorStats;

            // For charts
            vm.EditorNames = editorStats.Select(e => e.AuthorName).ToList();
            vm.EditorViews = editorStats.Select(e => e.TotalViews).ToList();
            vm.EditorAvgViews = editorStats.Select(e => e.AvgViewsPerArticle).ToList();

            // for CTR
            var data = await _analyticsService.GetDashboardAsync(selectedFromDate, selectedToDate);
            vm.AnalyticsData = data;

            // 🏆 Top editor
            var topEditor = editorStats.FirstOrDefault();
            ViewBag.TopEditor = topEditor?.AuthorName;
            //await RecalculateArticleScores();
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> OpenArticle(int id)
        {
            var article = await _articleService.GetEntityByIdAsync(id);

            if (article == null ||
                article.Category == null ||
                string.IsNullOrWhiteSpace(article.Category.Slug) ||
                string.IsNullOrWhiteSpace(article.Slug))
            {
                return NotFound();
            }

            var articleUrl = $"/news/{article.Category.Slug}/{article.Slug}";
            return Redirect(articleUrl);
        }

        public async Task<IActionResult> RecalculateArticleScores()
        {
            await _articleScoringService.RecalculateAllScoresAsync();

            return Ok("Article scores recalculated successfully.");
        }
    }
}
