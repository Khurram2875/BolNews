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

    /// <summary>Where the poll card sits: latest-top, latest-bottom, featured-top, featured-bottom or hidden.</summary>
    public string PollPlacement { get; set; } = "latest-top";
    /// <summary>Where the live events card sits (same values as <see cref="PollPlacement"/>).</summary>
    public string LivePlacement { get; set; } = "latest-top";

    // Older saved layouts used on/off switches; they are read once and folded into the placements by Normalize.
    public bool? ShowPoll { get; set; }
    public bool? ShowLiveEvents { get; set; }

    public int TopCardCount { get; set; } = 2;
    public bool ShowTrending { get; set; } = true;
    public int TrendingCount { get; set; } = 6;

    public bool FeaturedEnabled { get; set; } = true;
    public int FeaturedCount { get; set; } = 10;
    public bool ShowShows { get; set; } = true;

    public static readonly string[] Placements = { "latest-top", "latest-bottom", "featured-top", "featured-bottom", "hidden" };

    /// <summary>Column order from the start side (left in English, right in Urdu): any order of top, latest, featured.</summary>
    public string ColumnOrder { get; set; } = "latest,top,featured";

    // Older saved layouts used a swap switch; folded into ColumnOrder by Normalize.
    public bool? FeaturedRailFirst { get; set; }

    public IReadOnlyList<string> Columns => ColumnOrder.Split(',');

    /// <summary>True when the poll or live events card is placed in the given rail ("latest" or "featured").</summary>
    public bool HasWidgetIn(string rail) =>
        PollPlacement.StartsWith(rail + "-", StringComparison.Ordinal) || LivePlacement.StartsWith(rail + "-", StringComparison.Ordinal);

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
        if (s.ShowPoll == false) s.PollPlacement = "hidden";
        if (s.ShowLiveEvents == false) s.LivePlacement = "hidden";
        s.ShowPoll = null; s.ShowLiveEvents = null;
        if (!Placements.Contains(s.PollPlacement)) s.PollPlacement = "latest-top";
        if (!Placements.Contains(s.LivePlacement)) s.LivePlacement = "latest-top";

        if (s.FeaturedRailFirst == true && s.ColumnOrder == "latest,top,featured") s.ColumnOrder = "featured,top,latest";
        s.FeaturedRailFirst = null;
        var parts = (s.ColumnOrder ?? "").Split(',', StringSplitOptions.TrimEntries).Select(x => x.ToLowerInvariant()).ToList();
        var valid = parts.Count == 3 && parts.Distinct().Count() == 3 && parts.All(p => p is "top" or "latest" or "featured");
        s.ColumnOrder = valid ? string.Join(',', parts) : "latest,top,featured";

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
