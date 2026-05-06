using BolNews.Application.Models;

namespace BolNews.Application.Interfaces
{
    public interface ITrendingService
    {
        Task<List<TrendingTopicResult>> GetTrendingTopicsAsync();
    }
}
