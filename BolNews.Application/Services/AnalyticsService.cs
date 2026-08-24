using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsRepository _repo;

        public AnalyticsService(IAnalyticsRepository repo)
        {
            _repo = repo;
        }

        public Task TrackImpressionAsync(int articleId)
        {
            return _repo.IncrementImpressionAsync(
                articleId,
                DateTime.UtcNow.Date);
        }

        public Task TrackClickAsync(int articleId)
        {
            return _repo.IncrementClickAsync(
                articleId,
                DateTime.UtcNow.Date);
        }

        public async Task<double> GetCTRAsync(int articleId)
        {
            var data = await _repo.GetByArticleIdAsync(articleId);

            var impressions = data.Sum(a => a.Impressions);
            var clicks = data.Sum(a => a.Clicks);

            return impressions == 0
                ? 0
                : (double)clicks / impressions * 100;
        }

        public async Task<List<Article>> GetLowCTRArticlesAsync()
        {
            var ids = await _repo.GetLowCtrArticleIdsAsync(
                minImpressions: 100,
                maxCtrThreshold: 0.02);

            if (ids.Count == 0)
                return new List<Article>();

            return await _repo.GetArticlesByIdsAsync(ids);
        }

        public async Task<DashboardDto> GetDashboardAsync()
        {
            var data = await _repo.GetRecentWithArticlesAsync(
                DateTime.UtcNow.AddDays(-7));

            var grouped = data
                .GroupBy(x => x.ArticleId)
                .Select(g =>
                {
                    var impressions = g.Sum(x => x.Impressions);
                    var clicks = g.Sum(x => x.Clicks);

                    return new ArticlePerformanceDto
                    {
                        ArticleId = g.Key,

                        Title = g.First().Article?.Title
                                ?? string.Empty,

                        Clicks = clicks,

                        Impressions = impressions,

                        CTR = impressions == 0
                            ? 0
                            : (double)clicks / impressions
                    };
                })
                .OrderByDescending(x => x.CTR)
                .ToList();

            return new DashboardDto
            {
                TopArticles = grouped
                    .Take(10)
                    .ToList(),

                WorstArticles = grouped
                    .OrderBy(x => x.CTR)
                    .Take(10)
                    .ToList()
            };
        }

        public async Task<int> GetTotalImpressionsAsync(int articleId)
        {
            var data = await _repo.GetByArticleIdAsync(articleId);

            return data.Sum(x => x.Impressions);
        }

        public async Task<int> GetTotalClicksAsync(int articleId)
        {
            var data = await _repo.GetByArticleIdAsync(articleId);

            return data.Sum(x => x.Clicks);
        }
    }
}