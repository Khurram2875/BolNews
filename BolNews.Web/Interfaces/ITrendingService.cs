using BolNews.Web.Models;

namespace BolNews.Web.Interfaces
{
    public interface ITrendingService
    {
        Task<List<TrendingTopicResult>> GetTrendingTopicsAsync();
    }
}
