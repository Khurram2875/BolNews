using BolNews.Domain.Entities.Base;

namespace BolNews.Domain.Entities;

public class LiveEvent : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int FeaturedMediaId { get; set; }
    public MediaAsset FeaturedMedia { get; set; } = null!;
    public bool IsActive { get; set; }
    public bool IsPinned { get; set; }
    public bool ShowLiveBadge { get; set; }
    public ICollection<LiveEventUpdate> Updates { get; set; } = new List<LiveEventUpdate>();
    public ICollection<LiveEventArticle> LinkedArticles { get; set; } = new List<LiveEventArticle>();
}
