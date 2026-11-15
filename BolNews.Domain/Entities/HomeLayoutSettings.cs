using System.Text.Json;

namespace BolNews.Domain.Entities;

/// <summary>One category column in the home page category section.</summary>
public class HomeCategoryRow
{
    public string Slug { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int Count { get; set; } = 4;
    public int Order { get; set; }
}

/// <summary>
/// Publisher choices for the home page: which blocks show, how many stories each holds, which side the rails sit on
/// and which category columns appear in what order. Pinned stories always take priority over these counts.
/// The defaults reproduce the layout the site had before this setting existed.
/// </summary>
public class HomeLayoutSettings
{
    public static readonly string[] DefaultCategorySlugs =
        { "pakistan", "world", "business", "sports", "entertainment", "technology", "health", "lifestyle" };

    public bool LatestEnabled { get; set; } = true;
    public int LatestCount { get; set; } = 14;
    public bool ShowPoll { get; set; } = true;
    public bool ShowLiveEvents { get; set; } = true;

    public int TopCardCount { get; set; } = 2;
    public bool ShowTrending { get; set; } = true;
    public int TrendingCount { get; set; } = 6;

    public bool FeaturedEnabled { get; set; } = true;
    public int FeaturedCount { get; set; } = 10;
    public bool ShowShows { get; set; } = true;

    /// <summary>When true the Featured rail is on the start side (left in English, right in Urdu) and Latest on the other.</summary>
    public bool FeaturedRailFirst { get; set; }

    public bool CategoriesEnabled { get; set; } = true;

    /// <summary>Empty = the built-in default list (<see cref="DefaultCategorySlugs"/>, 4 stories each).</summary>
    public List<HomeCategoryRow> Categories { get; set; } = new();

    public bool CategoriesCustomized => Categories.Count > 0;

    public string ToJson() => JsonSerializer.Serialize(Normalize(this));

    public static HomeLayoutSettings FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new HomeLayoutSettings();
        try { return Normalize(JsonSerializer.Deserialize<HomeLayoutSettings>(json)); }
        catch (JsonException) { return new HomeLayoutSettings(); }
    }

    /// <summary>Clamps every number into its allowed range and tidies the category list (sorted by Order, unique slugs).</summary>
    public static HomeLayoutSettings Normalize(HomeLayoutSettings? value)
    {
        var s = value ?? new HomeLayoutSettings();
        s.LatestCount = Math.Clamp(s.LatestCount, 1, 20);
        s.TopCardCount = Math.Clamp(s.TopCardCount, 0, 6);
        s.TrendingCount = Math.Clamp(s.TrendingCount, 3, 10);
        s.FeaturedCount = Math.Clamp(s.FeaturedCount, 1, 15);
        s.Categories ??= new();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        s.Categories = s.Categories
            .Where(r => r is not null && !string.IsNullOrWhiteSpace(r.Slug))
            .Select(r => { r.Slug = r.Slug.Trim().ToLowerInvariant(); r.Count = Math.Clamp(r.Count, 1, 5); return r; })
            .Where(r => seen.Add(r.Slug))
            .OrderBy(r => r.Order)
            .Select((r, i) => { r.Order = i + 1; return r; })
            .ToList();
        return s;
    }
}
