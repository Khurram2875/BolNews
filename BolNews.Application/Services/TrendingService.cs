using BolNews.Application.Interfaces;
using BolNews.Application.Models;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Application.Services
{
    public class TrendingService : ITrendingService
    {
        private readonly IArticleService _articleService;
        private readonly IGoogleTrendsService _googleTrendsService;
        private readonly IMemoryCache _cache;

        public TrendingService(
            IArticleService articleService,
            IGoogleTrendsService googleTrendsService,
            IMemoryCache cache)
        {
            _articleService = articleService;
            _googleTrendsService = googleTrendsService;
            _cache = cache;
        }

        public async Task<List<TrendingTopicResult>> GetTrendingTopicsAsync()
        {
            var articles = await _articleService.GetRecentArticlesAsync();

            var stopWords = new[] { "the", "with", "this", "from", "that", "have" };

            var internalTrends = articles
                .SelectMany(a => a.Title.Split(' '))
                .Where(word => word.Length > 4 && !stopWords.Contains(word.ToLower()))
                .GroupBy(word => word.ToLower())
                .Select(g => new TrendingTopicResult
                {
                    Topic = g.Key,
                    Score = g.Count() * 2,
                    Source="System Rating"
                });

            var googleTrendTopics = await _googleTrendsService.GetTrendingTopicsAsync();

            var externalTrends = googleTrendTopics.Select(t =>
            {
                int score = 50;

                if (!string.IsNullOrEmpty(t.Traffic))
                {
                    var number = new string(t.Traffic.Where(char.IsDigit).ToArray());

                    if (int.TryParse(number, out int val))
                        score = val / 100;
                }

                return new TrendingTopicResult
                {
                    Topic = t.Title,
                    Score = score,
                    Source = t.Source,
                    Traffic = t.Traffic
                };
            });

            return internalTrends
                .Concat(externalTrends)
                .GroupBy(t => t.Topic.ToLower())
                .Select(g => new TrendingTopicResult
                {
                    Topic = g.First().Topic,
                    Score = g.Sum(x => x.Score),
                    Source = g.First().Source,
                    Traffic = g.First().Traffic
                })
                .OrderByDescending(x => x.Score)
                .Take(10)
                .ToList();
        }
    }
}
