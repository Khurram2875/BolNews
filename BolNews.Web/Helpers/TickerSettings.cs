namespace BolNews.Web.Helpers;

/// <summary>Headline ticker speed: an admin-chosen level 1 (slow) to 10 (fast), stored in SiteSettings.</summary>
public static class TickerSettings
{
    public const string SpeedKey = "Ticker.SpeedLevel";
    public const string CacheKey = "ticker_speed_level";
    public const int MinLevel = 1;
    public const int MaxLevel = 10;
    public const int DefaultLevel = 2;

    public static int Clamp(int level) => Math.Clamp(level, MinLevel, MaxLevel);

    public static int Parse(string? value) => int.TryParse(value, out var level) ? Clamp(level) : DefaultLevel;
}
