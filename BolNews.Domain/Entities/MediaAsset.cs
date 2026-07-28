using BolNews.Domain.Entities.Base;
using BolNews.Domain.Enums;

namespace BolNews.Domain.Entities;

public class MediaAsset : BaseEntity
{
    public MediaType MediaType { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? MediumUrl { get; set; }
    public string? LargeUrl { get; set; }
    public string? AltText { get; set; }
    public string? Caption { get; set; }
    public string? Credit { get; set; }
    public string? OriginalFileName { get; set; }
    public ICollection<MediaAssetTag> MediaAssetTags { get; set; } = new List<MediaAssetTag>();
    public ICollection<Article> FeaturedForArticles { get; set; } = new List<Article>();
}
