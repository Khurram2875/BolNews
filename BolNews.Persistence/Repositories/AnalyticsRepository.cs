using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Persistence.Repositories
{
    public class AnalyticsRepository : IAnalyticsRepository
    {
        private readonly AppDbContext _context;

        public AnalyticsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> IncrementImpressionAsync(
            int articleId,
            DateTime date)
        {
            const string sql = """
                INSERT INTO ArticleAnalytics
                    (ArticleId, Impressions, Clicks, Date)
                VALUES
                    ({0}, 1, 0, {1})
                ON DUPLICATE KEY UPDATE
                    Impressions = Impressions + 1;
                """;

            return await _context.Database.ExecuteSqlRawAsync(
                sql,
                articleId,
                date);
        }

        public async Task<int> IncrementClickAsync(
            int articleId,
            DateTime date)
        {
            const string sql = """
                INSERT INTO ArticleAnalytics
                    (ArticleId, Impressions, Clicks, Date)
                VALUES
                    ({0}, 0, 1, {1})
                ON DUPLICATE KEY UPDATE
                    Clicks = Clicks + 1;
                """;

            return await _context.Database.ExecuteSqlRawAsync(
                sql,
                articleId,
                date);
        }

        public async Task AddAsync(ArticleAnalytics record)
        {
            _context.ArticleAnalytics.Add(record);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ArticleAnalytics>> GetByArticleIdAsync(
            int articleId)
        {
            return await _context.ArticleAnalytics
                .AsNoTracking()
                .Where(a => a.ArticleId == articleId)
                .ToListAsync();
        }

        public async Task<List<ArticleAnalytics>> GetRecentWithArticlesAsync(
            DateTime since)
        {
            return await _context.ArticleAnalytics
                .AsNoTracking()
                .Where(x => x.Date >= since)
                .Include(x => x.Article)
                .ToListAsync();
        }

        public async Task<List<int>> GetLowCtrArticleIdsAsync(
            int minImpressions,
            double maxCtrThreshold)
        {
            var data = await _context.ArticleAnalytics
                .AsNoTracking()
                .GroupBy(a => a.ArticleId)
                .Select(g => new
                {
                    ArticleId = g.Key,
                    Impressions = g.Sum(x => x.Impressions),
                    Clicks = g.Sum(x => x.Clicks)
                })
                .Where(x =>
                    x.Impressions > minImpressions &&
                    (double)x.Clicks / x.Impressions < maxCtrThreshold)
                .ToListAsync();

            return data
                .Select(x => x.ArticleId)
                .ToList();
        }

        public async Task<List<Article>> GetArticlesByIdsAsync(
            List<int> ids)
        {
            if (ids.Count == 0)
                return new List<Article>();

            return await _context.Articles
                .AsNoTracking()
                .Where(a => ids.Contains(a.Id))
                .ToListAsync();
        }
    }
}