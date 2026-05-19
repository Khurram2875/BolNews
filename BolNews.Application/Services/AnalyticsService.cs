using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsRepository _repo;
        public AnalyticsService(IAnalyticsRepository repo) => _repo = repo;

        public async Task TrackImpressionAsync(int articleId)
        {
            var today   = DateTime.UtcNow.Date;
            var updated = await _repo.IncrementImpressionAsync(articleId, today);

            if (updated == 0)
            {
                // No row for today yet — insert with count 1.
                // If two requests race here, the second SaveChanges will throw a
                // unique constraint violation (ArticleId + Date). We catch it and
                // retry the increment so no impression is lost.
                try
                {
                    await _repo.AddAsync(new ArticleAnalytics
                    {
                        ArticleId   = articleId,
                        Date        = today,
                        Impressions = 1,
                        Clicks      = 0
                    });
                }
                catch
                {
                    await _repo.IncrementImpressionAsync(articleId, today);
                }
            }
        }

        public async Task TrackClickAsync(int articleId)
        {
            var today   = DateTime.UtcNow.Date;
            var updated = await _repo.IncrementClickAsync(articleId, today);

            if (updated == 0)
            {
                try
                {
                    await _repo.AddAsync(new ArticleAnalytics
                    {
                        ArticleId = articleId,
                        Date      = today,
                        Clicks    = 1,
                        Impressions = 0
                    });
                }
                catch
                {
                    await _repo.IncrementClickAsync(articleId, today);
                }
            }
        }

        public async Task<double> GetCTRAsync(int articleId)
        {
            var data        = await _repo.GetByArticleIdAsync(articleId);
            var impressions = data.Sum(a => a.Impressions);
            var clicks      = data.Sum(a => a.Clicks);
            return impressions == 0 ? 0 : (double)clicks / impressions * 100;
        }

        public async Task<List<Article>> GetLowCTRArticlesAsync()
        {
            var ids = await _repo.GetLowCtrArticleIdsAsync(
                minImpressions: 100, maxCtrThreshold: 0.02);
            return await _repo.GetArticlesByIdsAsync(ids);
        }

        public async Task<DashboardDto> GetDashboardAsync()
        {
            var data    = await _repo.GetRecentWithArticlesAsync(DateTime.UtcNow.AddDays(-7));
            var grouped = data
                .GroupBy(x => x.ArticleId)
                .Select(g => new ArticlePerformanceDto
                {
                    ArticleId   = g.Key,
                    Title       = g.First().Article?.Title ?? string.Empty,
                    Clicks      = g.Sum(x => x.Clicks),
                    Impressions = g.Sum(x => x.Impressions),
                    CTR         = g.Sum(x => x.Impressions) == 0
                        ? 0 : (double)g.Sum(x => x.Clicks) / g.Sum(x => x.Impressions)
                })
                .OrderByDescending(x => x.CTR)
                .ToList();

            return new DashboardDto
            {
                TopArticles   = grouped.Take(10).ToList(),
                WorstArticles = grouped.OrderBy(x => x.CTR).Take(10).ToList()
            };
        }
    }
}
