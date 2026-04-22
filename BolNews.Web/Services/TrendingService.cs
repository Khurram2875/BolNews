using BolNews.Application.Interfaces;
using BolNews.Web.Interfaces;
using BolNews.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Web.Services
{
    public class TrendingService : ITrendingService
    {
        private readonly IArticleService _articleService;
        private readonly IGoogleTrendsService _googleTrendsService;
        private readonly IMemoryCache _cache;

        public TrendingService(IArticleService articleService, IGoogleTrendsService googleTrendsService, IMemoryCache cache)
        {
            _articleService = articleService;
            _googleTrendsService = googleTrendsService;
            _cache = cache;
        }

        public async Task<List<TrendingTopicResult>> GetTrendingTopicsAsync()
        {
            var articles = await _articleService.GetRecentArticlesAsync(); // last 24–48h

            var internalTrends = articles
                 .SelectMany(a => a.Title.Split(' '))
                 .Where(word => word.Length > 4)
                 .GroupBy(word => word.ToLower())
                 .Select(g => new TrendingTopicResult
                 {
                     Topic = g.Key,
                     Score = g.Count() * 2
                 });
            var googleTrends = await _cache.GetOrCreateAsync("google_trends", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                return await _googleTrendsService.GetTrendingTopicsAsync("PK");
            });
            var externalTrends = googleTrends.Select(t => new TrendingTopicResult
            {
                Topic = t,
                Score = 50 // base boost
            });

            var combined = internalTrends
            .Concat(externalTrends)
            .GroupBy(t => t.Topic.ToLower())
            .Select(g => new TrendingTopicResult
            {
                Topic = g.First().Topic,
                Score = g.Sum(x => x.Score)
            })
            .OrderByDescending(x => x.Score)
            .Take(10)
            .ToList();
            if(externalTrends==null)
            {
                 combined = internalTrends.OrderByDescending(x => x.Score)
                .Take(10)
                .ToList();
            }

            return (combined);


            
        }
    }
}
