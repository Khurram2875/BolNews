using System.Text.Json;

namespace BolNews.Domain.Entities;

/// <summary>
/// Publisher-written HTML for a fixed set of places on the public site. Each slot holds already-sanitised HTML
/// (the web layer cleans it on save and again on render). Empty slot = nothing rendered.
/// </summary>
public class CustomBlocks
{
    public const int MaxLength = 20_000;

    public string? HeaderAbove { get; set; }
    public string? HeaderBelow { get; set; }
    public string? ArticleAbove { get; set; }
    public string? ArticleBelow { get; set; }
    public string? ArticleSidebar { get; set; }
    public string? FooterTop { get; set; }
    public string? FooterBottom { get; set; }

    /// <summary>Slot key, label shown to the publisher, and where it appears.</summary>
    public static readonly (string Key, string Label, string Where)[] Slots =
    {
        (nameof(HeaderAbove), "Above header", "Very top of every public page, above the logo bar."),
        (nameof(HeaderBelow), "Below header", "Directly under the header/navigation, above page content."),
        (nameof(ArticleAbove), "Above article text", "Article pages: between the headline/image and the article text."),
        (nameof(ArticleBelow), "Below article text", "Article pages: after the article text, before the tags."),
        (nameof(ArticleSidebar), "Article sidebar", "Article pages: top of the right-hand sidebar."),
        (nameof(FooterTop), "Above footer", "Every public page, just above the footer."),
        (nameof(FooterBottom), "Below footer", "Very bottom of every public page.")
    };

    public string? Get(string key) => key switch
    {
        nameof(HeaderAbove) => HeaderAbove, nameof(HeaderBelow) => HeaderBelow,
        nameof(ArticleAbove) => ArticleAbove, nameof(ArticleBelow) => ArticleBelow,
        nameof(ArticleSidebar) => ArticleSidebar, nameof(FooterTop) => FooterTop,
        nameof(FooterBottom) => FooterBottom, _ => null
    };

    public void Set(string key, string? value)
    {
        switch (key)
        {
            case nameof(HeaderAbove): HeaderAbove = value; break;
            case nameof(HeaderBelow): HeaderBelow = value; break;
            case nameof(ArticleAbove): ArticleAbove = value; break;
            case nameof(ArticleBelow): ArticleBelow = value; break;
            case nameof(ArticleSidebar): ArticleSidebar = value; break;
            case nameof(FooterTop): FooterTop = value; break;
            case nameof(FooterBottom): FooterBottom = value; break;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static CustomBlocks FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new CustomBlocks();
        try { return JsonSerializer.Deserialize<CustomBlocks>(json) ?? new CustomBlocks(); }
        catch (JsonException) { return new CustomBlocks(); }
    }
}
