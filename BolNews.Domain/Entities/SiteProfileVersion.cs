namespace BolNews.Domain.Entities;

/// <summary>A saved state of the publisher style settings, so a bad edit can be rolled back.</summary>
public class SiteProfileVersion
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string Note { get; set; } = "Saved";

    /// <summary>JSON of <see cref="SiteProfileStyleSnapshot"/>.</summary>
    public string SnapshotJson { get; set; } = "{}";
}

/// <summary>The style-related fields of the site profile that a version captures and restores.</summary>
public class SiteProfileStyleSnapshot
{
    public string? CustomCss { get; set; }
    public string? StyleSettingsJson { get; set; }
    public string ThemeName { get; set; } = "default";
    public string PrimaryColor { get; set; } = "#d30b12";
    public string AccentColor { get; set; } = "#d30b12";
    public string FontFamily { get; set; } = "Inter";
    public int BaseFontSizePx { get; set; } = 16;
    public int ArticleFontSizePx { get; set; }
}
