using System.Text.Json;
using System.Text.RegularExpressions;

namespace BolNews.Domain.Entities;

/// <summary>One text element. Every value is optional: null means "keep the theme default".</summary>
public class ElementStyle
{
    public int? SizePx { get; set; }
    public int? Weight { get; set; }
    public string? Color { get; set; }
    public decimal? LineHeight { get; set; }

    public bool IsEmpty => SizePx is null && Weight is null && string.IsNullOrWhiteSpace(Color) && LineHeight is null;
}

/// <summary>
/// Publisher-controlled typography and colour tweaks, stored as JSON on the site profile (StyleSettingsJson)
/// and turned into CSS at render time. Nothing here can contain markup: only validated numbers and #RRGGBB colours.
/// </summary>
public class StyleSettings
{
    public ElementStyle ArticleHeadline { get; set; } = new();
    public ElementStyle BodyH1 { get; set; } = new();
    public ElementStyle BodyH2 { get; set; } = new();
    public ElementStyle BodyH3 { get; set; } = new();
    public ElementStyle BodyH4 { get; set; } = new();
    public ElementStyle SectionTitle { get; set; } = new();
    public decimal? ArticleLineHeight { get; set; }
    public string? ArticleLinkColor { get; set; }
    public int? CornerRadiusPx { get; set; }
    /// <summary>Colour of the sentence highlighted during read-aloud (#RRGGBB); empty keeps the default yellow.</summary>
    public string? ReadAloudHighlightColor { get; set; }
    /// <summary>Whether the page scrolls to follow the sentence being read.</summary>
    public bool ReadAloudAutoScroll { get; set; } = true;
    /// <summary>Master switch for the publisher's additional CSS. Turning it off restores the built-in look without deleting the CSS.</summary>
    public bool CustomCssEnabled { get; set; } = true;

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    public string ToJson() => JsonSerializer.Serialize(this, Options);

    public static StyleSettings FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new StyleSettings();
        try { return Normalize(JsonSerializer.Deserialize<StyleSettings>(json, Options)); }
        catch (JsonException) { return new StyleSettings(); }
    }

    /// <summary>Makes sure no nested object is null (JSON "null" or missing sections).</summary>
    public static StyleSettings Normalize(StyleSettings? value)
    {
        var s = value ?? new StyleSettings();
        s.ArticleHeadline ??= new(); s.BodyH1 ??= new(); s.BodyH2 ??= new(); s.BodyH3 ??= new(); s.BodyH4 ??= new(); s.SectionTitle ??= new();
        return s;
    }

    public IEnumerable<(string Field, ElementStyle Style)> Elements()
    {
        yield return (nameof(ArticleHeadline), ArticleHeadline);
        yield return (nameof(BodyH1), BodyH1);
        yield return (nameof(BodyH2), BodyH2);
        yield return (nameof(BodyH3), BodyH3);
        yield return (nameof(BodyH4), BodyH4);
        yield return (nameof(SectionTitle), SectionTitle);
    }

    private static bool Hex(string? v) => string.IsNullOrWhiteSpace(v) || Regex.IsMatch(v, "^#[0-9a-fA-F]{6}$");

    /// <summary>Returns human-readable problems; an empty list means the settings are safe to store and render.</summary>
    public List<string> Validate()
    {
        var errors = new List<string>();
        foreach (var (field, e) in Elements())
        {
            if (e.SizePx is < 10 or > 80) errors.Add($"{field}: size must be 10–80 px.");
            if (e.Weight is not null && (e.Weight < 100 || e.Weight > 900 || e.Weight % 100 != 0)) errors.Add($"{field}: weight must be 100, 200 … 900.");
            if (!Hex(e.Color)) errors.Add($"{field}: colour must look like #RRGGBB.");
            if (e.LineHeight is < 1m or > 3.5m) errors.Add($"{field}: line height must be 1.0–3.5.");
        }
        if (ArticleLineHeight is < 1m or > 3.5m) errors.Add("Article line height must be 1.0–3.5.");
        if (!Hex(ArticleLinkColor)) errors.Add("Article link colour must look like #RRGGBB.");
        if (!Hex(ReadAloudHighlightColor)) errors.Add("Read-aloud highlight colour must look like #RRGGBB.");
        if (CornerRadiusPx is < 0 or > 32) errors.Add("Corner roundness must be 0–32 px.");
        return errors;
    }
}
