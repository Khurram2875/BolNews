namespace BolNews.Web.Models
{
    public class HeadlineSuggestionResult
    {
        public string Original { get; set; }
        public List<string> Suggestions { get; set; } = new();
    }
}
