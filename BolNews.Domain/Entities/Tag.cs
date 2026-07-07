using BolNews.Domain.Entities.Base;

namespace BolNews.Domain.Entities
{
    public class Tag : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public string NormalizedName { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();

        public ICollection<FeaturedImageTag> FeaturedImageTags { get; set; } = new List<FeaturedImageTag>();
    }
}
