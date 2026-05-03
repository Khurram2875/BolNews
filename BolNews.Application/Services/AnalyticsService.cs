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

            var updated = await _context.ArticleAnalytics
                .Where(a => a.ArticleId == articleId && a.Date == today)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.Impressions, a => a.Impressions + 1));

            if (updated == 0)
            {
                // No row yet for today — insert one. Handle the rare concurrent-insert case.
                try
                {
                    _context.ArticleAnalytics.Add(new ArticleAnalytics
                    {
                        ArticleId   = articleId,
                        Date        = today,
                        Impressions = 1
                    });
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Another request inserted the row between our check and insert.
                    // Retry the atomic increment — this path is extremely rare.
                    await _context.ArticleAnalytics
                        .Where(a => a.ArticleId == articleId && a.Date == today)
                        .ExecuteUpdateAsync(s => s.SetProperty(a => a.Impressions, a => a.Impressions + 1));
                }
            }
        }

        public async Task TrackClickAsync(int articleId)
        {
            var today = DateTime.UtcNow.Date;

            var updated = await _context.ArticleAnalytics
                .Where(a => a.ArticleId == articleId && a.Date == today)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.Clicks, a => a.Clicks + 1));

            if (updated == 0)
            {
                try
                {
                    _context.ArticleAnalytics.Add(new ArticleAnalytics
                    {
                        ArticleId = articleId,
                        Date      = today,
                        Clicks    = 1
                    });
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    await _context.ArticleAnalytics
                        .Where(a => a.ArticleId == articleId && a.Date == today)
                        .ExecuteUpdateAsync(s => s.SetProperty(a => a.Clicks, a => a.Clicks + 1));
                }
            }
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
    }
}
