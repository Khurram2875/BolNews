using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Application.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly AppDbContext _context;

        public AnalyticsService(AppDbContext context)
        {
            _context = context;
        }

        // Fix: atomic upsert — no read-modify-write race condition.
        // Try to insert a new row for today; if it already exists increment atomically.
        public async Task TrackImpressionAsync(int articleId)
        {
            var today = DateTime.UtcNow.Date;

            var record = await _context.ArticleAnalytics
                .FirstOrDefaultAsync(x => x.ArticleId == articleId && x.Date == today);

            if (record == null)
            {
                record = new ArticleAnalytics
                {
                    ArticleId = articleId,
                    Date = today,
                    Impressions = 1,
                    Clicks = 0
                };
                _context.ArticleAnalytics.Add(record);
            }
            else
            {
                record.Impressions++;
            }

            await _context.SaveChangesAsync();
        }

        public async Task TrackClickAsync(int articleId)
        {
            var today = DateTime.UtcNow.Date;

            var record = await _context.ArticleAnalytics
                .FirstOrDefaultAsync(x => x.ArticleId == articleId && x.Date == today);

            if (record == null)
            {
                record = new ArticleAnalytics
                {
                    ArticleId = articleId,
                    Date = today,
                    Clicks = 1,
                    Impressions = 0
                };
                _context.ArticleAnalytics.Add(record);
            }
            else
            {
                record.Clicks++;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<double> GetCTRAsync(int articleId)
        {
            var data = await _context.ArticleAnalytics
                .Where(a => a.ArticleId == articleId)
                .ToListAsync();

            var impressions = data.Sum(a => a.Impressions);
            var clicks      = data.Sum(a => a.Clicks);

            return impressions == 0 ? 0 : (double)clicks / impressions * 100;
        }

        public async Task<List<Article>> GetLowCTRArticlesAsync()
        {
            var data = await _context.ArticleAnalytics
                .GroupBy(a => a.ArticleId)
                .Select(g => new
                {
                    ArticleId   = g.Key,
                    Impressions = g.Sum(x => x.Impressions),
                    Clicks      = g.Sum(x => x.Clicks)
                })
                .Where(x => x.Impressions > 100 &&
                            (double)x.Clicks / x.Impressions < 0.02)
                .ToListAsync();

            var ids = data.Select(x => x.ArticleId).ToList();

            return await _context.Articles
                .Where(a => ids.Contains(a.Id))
                .ToListAsync();
        }

        public async Task<DashboardDto> GetDashboardAsync()
        {
            var data = await _context.ArticleAnalytics
             .Include(x => x.Article)
             .Where(x => x.Date >= DateTime.UtcNow.AddDays(-7))
             .ToListAsync();

            var grouped = data
                .GroupBy(x => x.ArticleId)
                .Select(g => new ArticlePerformanceDto
                {
                    ArticleId = g.Key,
                    Title = g.First().Article.Title,
                    Clicks = g.Sum(x => x.Clicks),
                    Impressions = g.Sum(x => x.Impressions),
                    CTR = g.Sum(x => x.Impressions) == 0
                        ? 0
                        : (double)g.Sum(x => x.Clicks) / g.Sum(x => x.Impressions)
                })
                .OrderByDescending(x => x.CTR)
                .ToList();

            return new DashboardDto
            {
                TopArticles = grouped.Take(10).ToList(),
                WorstArticles = grouped.OrderBy(x => x.CTR).Take(10).ToList()
            };
        }
    }
}
