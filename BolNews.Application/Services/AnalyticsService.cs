using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public async Task TrackImpressionAsync(int articleId)
        {
            var today = DateTime.UtcNow.Date;

            var record = await _context.ArticleAnalytics
                .FirstOrDefaultAsync(a => a.ArticleId == articleId && a.Date == today);

            if (record == null)
            {
                record = new ArticleAnalytics
                {
                    ArticleId = articleId,
                    Date = today,
                    Impressions = 1
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
                .FirstOrDefaultAsync(a => a.ArticleId == articleId && a.Date == today);

            if (record == null)
            {
                record = new ArticleAnalytics
                {
                    ArticleId = articleId,
                    Date = today,
                    Clicks = 1
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
            var clicks = data.Sum(a => a.Clicks);

            return impressions == 0 ? 0 : (double)clicks / impressions * 100;
        }

        public async Task<List<Article>> GetLowCTRArticlesAsync()
        {

            var d1 = await _context.ArticleAnalytics.ToListAsync();
            var data = await _context.ArticleAnalytics
                .GroupBy(a => a.ArticleId)
                .Select(g => new
                {
                    ArticleId = g.Key,
                    Impressions = g.Sum(x => x.Impressions),
                    Clicks = g.Sum(x => x.Clicks)
                })
                .Where(x => x.Impressions > 100 && (double)x.Clicks / x.Impressions < 0.02) // <2% CTR
                .ToListAsync();

            var ids = data.Select(x => x.ArticleId).ToList();

            return await _context.Articles
                .Where(a => ids.Contains(a.Id))
                .ToListAsync();
        }
    }
}
