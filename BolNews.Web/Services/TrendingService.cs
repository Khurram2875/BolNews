using BolNews.Application.Interfaces;
using BolNews.Web.Interfaces;
using BolNews.Web.Models;

namespace BolNews.Web.Services
{
    public class TrendingService : ITrendingService
    {
        private readonly IArticleService _articleService;

        public TrendingService(IArticleService articleService)
        {
            _articleService = articleService;
        }

        public async Task<List<TrendingTopicResult>> GetTrendingTopicsAsync()
        {
            var articles = await _articleService.GetRecentArticlesAsync(); // last 24–48h

            var trending = articles
                 .SelectMany(a => a.Title.Split(' '))
                 .Where(word => word.Length > 4)
                 .GroupBy(word => word.ToLower())
                 .Select(g => new TrendingTopicResult
                 {
                     Topic = g.Key,
                     Score = g.Count()
                 })
                 .OrderByDescending(x => x.Score)
                 .Take(10)
                 .ToList();
            return trending;
        }
    }
}
