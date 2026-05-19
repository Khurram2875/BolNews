using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Persistence.Repositories
{
    public class AnalyticsRepository : IAnalyticsRepository
    {
        private readonly AppDbContext _context;
        public AnalyticsRepository(AppDbContext context) => _context = context;

        // ── Atomic increments ─────────────────────────────────────────────────
        //
        // ExecuteUpdateAsync translates to a single SQL UPDATE:
        //   UPDATE ArticleAnalytics SET Impressions = Impressions + 1
        //   WHERE ArticleId = @id AND DATE(Date) = DATE(@date)
        //
        // Using EF.Functions.DateDiffDay(a.Date, date) == 0 is not cross-provider.
        // Instead we compare a.Date >= startOfDay AND a.Date < startOfNextDay —
        // this works on both MariaDB (native DATETIME) and SQLite (ISO-8601 text).

        public async Task<int> IncrementImpressionAsync(int articleId, DateTime date)
        {
            var start = date.Date;
            var end   = start.AddDays(1);

            return await _context.ArticleAnalytics
                .Where(a => a.ArticleId == articleId
                         && a.Date >= start
                         && a.Date < end)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(a => a.Impressions, a => a.Impressions + 1));
        }

        public async Task<int> IncrementClickAsync(int articleId, DateTime date)
        {
            var start = date.Date;
            var end   = start.AddDays(1);

            return await _context.ArticleAnalytics
                .Where(a => a.ArticleId == articleId
                         && a.Date >= start
                         && a.Date < end)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(a => a.Clicks, a => a.Clicks + 1));
        }

        // ── Other methods ─────────────────────────────────────────────────────

        public async Task AddAsync(ArticleAnalytics record)
        {
            _context.ArticleAnalytics.Add(record);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ArticleAnalytics>> GetByArticleIdAsync(int articleId)
            => await _context.ArticleAnalytics
                .Where(a => a.ArticleId == articleId)
                .ToListAsync();

        public async Task<List<ArticleAnalytics>> GetRecentWithArticlesAsync(DateTime since)
            => await _context.ArticleAnalytics
                .Include(x => x.Article)
                .Where(x => x.Date >= since)
                .ToListAsync();

        public async Task<List<int>> GetLowCtrArticleIdsAsync(
            int minImpressions, double maxCtrThreshold)
        {
            var data = await _context.ArticleAnalytics
                .GroupBy(a => a.ArticleId)
                .Select(g => new
                {
                    ArticleId   = g.Key,
                    Impressions = g.Sum(x => x.Impressions),
                    Clicks      = g.Sum(x => x.Clicks)
                })
                .Where(x => x.Impressions > minImpressions &&
                            (double)x.Clicks / x.Impressions < maxCtrThreshold)
                .ToListAsync();

            return data.Select(x => x.ArticleId).ToList();
        }

        public async Task<List<Article>> GetArticlesByIdsAsync(List<int> ids)
            => await _context.Articles
                .Where(a => ids.Contains(a.Id))
                .ToListAsync();
    }
}
