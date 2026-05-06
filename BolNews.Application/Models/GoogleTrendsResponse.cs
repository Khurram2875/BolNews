namespace BolNews.Application.Models
{
    internal sealed class GoogleTrendsResponse
    {
        public List<TrendItem> Trends { get; set; }
    }

    internal sealed class TrendItem
    {
        public string Title { get; set; }
    }
}
