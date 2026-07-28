using BolNews.Domain.Enums;

namespace BolNews.Application.DTOs;

public class MediaAssetDto
{
    public int Id { get; set; }
    public MediaType MediaType { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? MediumUrl { get; set; }
    public string? LargeUrl { get; set; }
    public string? AltText { get; set; }
    public string? Caption { get; set; }
    public string? Credit { get; set; }
    public string? OriginalFileName { get; set; }
    public string? TagsInput { get; set; }
    public List<TagDto> Tags { get; set; } = new();
    public List<string> UsedByArticles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
