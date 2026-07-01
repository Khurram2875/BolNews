using BolNews.Domain.Entities.Base;

namespace BolNews.Domain.Entities
{
    public class EditorialPlacement : BaseEntity
    {
        public string PlacementKey { get; set; } = string.Empty;

        public int ArticleId { get; set; }

        public Article Article { get; set; } = null!;

        public int SortOrder { get; set; }
    }
}
