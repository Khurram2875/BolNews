using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;

namespace BolNews.Application.Interfaces
{
    public interface IAnalyticsRepository
    {
        Task<int> IncrementImpressionAsync(int articleId, DateTime date);
        Task<int> IncrementClickAsync(int articleId, DateTime date);
        Task<List<ArticleAnalytics>> GetByArticleIdAsync(int articleId);
        Task<List<ArticleAnalytics>> GetRecentWithArticlesAsync(DateTime since);
        Task<List<int>> GetLowCtrArticleIdsAsync(int minImpressions, double maxCtrThreshold);
        Task<List<Article>> GetArticlesByIdsAsync(List<int> ids);
        
    }
}
