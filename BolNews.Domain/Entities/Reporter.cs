using BolNews.Domain.Entities.Base;

namespace BolNews.Domain.Entities
{
    public class Reporter : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public string? SourceName { get; set; }

        public string? Slug { get; set; }

        public ICollection<Article> Articles { get; set; } = new List<Article>();
    }
}
