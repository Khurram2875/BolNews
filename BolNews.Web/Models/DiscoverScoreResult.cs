namespace BolNews.Web.Models
{
    public class DiscoverScoreResult
    {
        public int Score { get; set; }

        public bool HasGoodTitle { get; set; }
        public bool HasLargeImage { get; set; }
        public bool IsFresh { get; set; }
        public bool HasMetaDescription { get; set; }
        public bool HasStructuredData { get; set; }

        public List<string> Suggestions { get; set; } = new();
    }
}
