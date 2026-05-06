namespace BolNews.Application.Interfaces
{
    public interface IGoogleTrendsService
    {
        Task<List<string>> GetTrendingTopicsAsync(string geo = "PK");
    }
}
