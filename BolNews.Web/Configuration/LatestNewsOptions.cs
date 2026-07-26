namespace BolNews.Web.Configuration
{
    public class LatestNewsOptions
    {
        public const string SectionName = "LatestNews";

        public int DefaultCount { get; set; } = 20;
        public string[] IncludedCategorySlugs { get; set; } = Array.Empty<string>();
        public string[] ExcludedCategorySlugs { get; set; } = Array.Empty<string>();
    }
}
