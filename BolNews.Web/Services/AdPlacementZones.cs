namespace BolNews.Web.Services;

public sealed record AdPlacementZone(string Key, string Name, bool IsInline = false);

public static class AdPlacementZones
{
    public static readonly IReadOnlyList<AdPlacementZone> All =
    [
        new("site.top", "Site: above page content"),
        new("site.bottom", "Site: before footer"),
        new("home.sidebar.top", "Homepage: sidebar top"),
        new("home.sidebar.bottom", "Homepage: sidebar bottom"),
        new("home.content", "Homepage: between content sections"),
        new("article.sidebar.top", "Article: sidebar top"),
        new("article.sidebar.bottom", "Article: sidebar bottom"),
        new("article.inline", "Article: inside article body", true),
        new("category.sidebar.top", "Category: sidebar top"),
        new("category.sidebar.bottom", "Category: sidebar bottom"),
        new("category.content", "Category: between content sections"),
        new("tag.sidebar.top", "Tag: sidebar top"),
        new("tag.sidebar.bottom", "Tag: sidebar bottom"),
        new("tag.content", "Tag: between content sections"),
        new("search.sidebar.top", "Search: sidebar top"),
        new("search.sidebar.bottom", "Search: sidebar bottom"),
        new("search.content", "Search: between results"),
        new("video.sidebar.top", "Video: sidebar top")
    ];

    public static bool Contains(string key) => All.Any(x => x.Key == key);
    public static bool IsInline(string key) => All.FirstOrDefault(x => x.Key == key)?.IsInline == true;
}
