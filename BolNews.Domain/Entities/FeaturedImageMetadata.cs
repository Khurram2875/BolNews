using BolNews.Domain.Entities.Base;

namespace BolNews.Domain.Entities
{
    public class FeaturedImageMetadata : BaseEntity
    {
        public int ArticleId { get; set; }

        public Article Article { get; set; } = null!;

        public string? AltText { get; set; }

        public string? Caption { get; set; }

        public string? Credit { get; set; }

        public ICollection<FeaturedImageTag> FeaturedImageTags { get; set; } = new List<FeaturedImageTag>();
    }
}
