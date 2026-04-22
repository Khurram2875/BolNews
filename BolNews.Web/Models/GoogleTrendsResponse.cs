namespace BolNews.Web.Models
{
    public class GoogleTrendsResponse
    {
        public List<TrendItem> Trends { get; set; }
    }

    public class TrendItem
    {
        public string Title { get; set; }
    }
}
