namespace BolNews.Domain.Entities;

/// <summary>Small admin-editable key/value setting (e.g. the headline ticker speed).</summary>
public class SiteSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
