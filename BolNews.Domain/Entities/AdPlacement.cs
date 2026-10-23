namespace BolNews.Domain.Entities;

/// <summary>
/// An ad unit configured for a supported public-page zone. The zone is chosen
/// by an administrator; views expose the available zones as stable anchors.
/// </summary>
public class AdPlacement
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ZoneKey { get; set; } = string.Empty;
    public string ElementId { get; set; } = string.Empty;
    public string ProviderUnit { get; set; } = string.Empty;
    public string ContainerClass { get; set; } = string.Empty;
    public string? TargetSelector { get; set; }
    public string TargetPosition { get; set; } = "beforeend";
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public int? ParagraphIndex { get; set; }
    public string InsertionMode { get; set; } = "zone";
    public DateTime UpdatedAt { get; set; }
}
