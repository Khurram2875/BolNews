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

        // SQLite stores DateTime as text. To guarantee the WHERE clause matches,
        // we compare the Date column using EF.Functions.Like on the date prefix,
        // which is reliable across both SQLite (text) and MariaDB (native date).
        // The date string format is always "yyyy-MM-dd".
        private static string DateKey(DateTime date) => date.ToString("yyyy-MM-dd");

        public async Task<int> IncrementImpressionAsync(int articleId, DateTime date)
        {
            var records = await _context.ArticleAnalytics
                .Where(a => a.ArticleId == articleId && a.Date.Date == date.Date)
                .ToListAsync();

            if (records.Count == 0) return 0;

            foreach (var r in records)
                r.Impressions++;

            await _context.SaveChangesAsync();
            return records.Count;
        }

        public async Task<int> IncrementClickAsync(int articleId, DateTime date)
        {
            var records = await _context.ArticleAnalytics
                .Where(a => a.ArticleId == articleId && a.Date.Date == date.Date)
                .ToListAsync();

            if (records.Count == 0) return 0;

            foreach (var r in records)
                r.Clicks++;

            await _context.SaveChangesAsync();
            return records.Count;
        }

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
