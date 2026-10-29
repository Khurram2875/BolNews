using BolNews.Domain.Entities.Base;
using BolNews.Domain.Enums;

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
    public LiveEventType EventType { get; set; } = LiveEventType.Standard;
    // Election scoreboard card labels (only used when EventType is Election).
    public string? ElectionHeader { get; set; }
    public string? RaceTitle { get; set; }
    public string? RaceSubtitle { get; set; }
    public ICollection<LiveEventCandidate> Candidates { get; set; } = new List<LiveEventCandidate>();
    public ICollection<LiveEventUpdate> Updates { get; set; } = new List<LiveEventUpdate>();
    public ICollection<LiveEventArticle> LinkedArticles { get; set; } = new List<LiveEventArticle>();
}
