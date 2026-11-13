using System.Text.RegularExpressions;
using BolNews.Domain.Entities;

namespace BolNews.Web.Services;

public static class PublisherThemeCatalog
{
    private static readonly IReadOnlyDictionary<string, string> FontStacks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Inter"] = "'Inter', Arial, sans-serif",
        ["Poppins"] = "'Poppins', Arial, sans-serif",
        ["Roboto"] = "'Roboto', Arial, sans-serif",
        ["Open Sans"] = "'Open Sans', Arial, sans-serif",
        ["Lato"] = "'Lato', Arial, sans-serif",
        ["Merriweather"] = "'Merriweather', Georgia, serif",
        ["Playfair Display"] = "'Playfair Display', Georgia, serif",
        ["Source Sans 3"] = "'Source Sans 3', Arial, sans-serif",
        ["System UI"] = "system-ui, -apple-system, 'Segoe UI', sans-serif",
        ["Georgia"] = "Georgia, 'Times New Roman', serif",
        ["Arial"] = "Arial, Helvetica, sans-serif",
        ["Noto Nastaliq Urdu"] = "'Noto Nastaliq Urdu', 'Noto Naskh Arabic', serif",
        ["Noto Naskh Arabic"] = "'Noto Naskh Arabic', 'Noto Nastaliq Urdu', serif",
        ["Gulzar"] = "'Gulzar', 'Noto Nastaliq Urdu', serif",
        ["Lateef"] = "'Lateef', 'Noto Naskh Arabic', serif",
        ["Scheherazade New"] = "'Scheherazade New', 'Noto Naskh Arabic', serif",
        ["Amiri"] = "'Amiri', 'Noto Naskh Arabic', serif",
        ["Jameel Noori Nastaleeq"] = "'Jameel Noori Nastaleeq', 'Noto Nastaliq Urdu', 'Noto Naskh Arabic', serif",
        ["Alvi Nastaleeq"] = "'Alvi Nastaleeq', 'Noto Nastaliq Urdu', 'Noto Naskh Arabic', serif"
    };

    /// <summary>
    /// Fonts served from this site (wwwroot/fonts) instead of Google Fonts: family name -> (file, css format).
    /// A local font only appears in the Publisher Settings list when its file is actually present.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (string File, string Format)> LocalFonts =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Jameel Noori Nastaleeq"] = ("JameelNooriNastaleeq.woff2", "woff2"),
            ["Alvi Nastaleeq"] = ("AlviNastaleeq.ttf", "truetype")
        };

    private static string? _webRoot;

    /// <summary>Call once at startup so local fonts can be checked on disk.</summary>
    public static void ConfigureWebRoot(string? webRootPath) => _webRoot = webRootPath;

    private static bool IsAvailable(string name) =>
        !LocalFonts.TryGetValue(name, out var local) ||
        (_webRoot != null && File.Exists(Path.Combine(_webRoot, "fonts", local.File)));

    public static IReadOnlyCollection<string> Languages { get; } = new[] { "en", "ur" };
    private static readonly string[] UrduFonts =
    {
        "Noto Nastaliq Urdu", "Noto Naskh Arabic", "Gulzar", "Lateef", "Scheherazade New", "Amiri",
        "Jameel Noori Nastaleeq", "Alvi Nastaleeq"
    };

    public static string NormalizeLanguage(string? value) =>
        Languages.FirstOrDefault(code => string.Equals(code, value, StringComparison.OrdinalIgnoreCase)) ?? "en";

    /// <summary>Urdu (and other right-to-left languages) render right-to-left.</summary>
    public static bool IsRtl(string? languageCode) => NormalizeLanguage(languageCode) == "ur";

    public static string TextDirection(string? languageCode) => IsRtl(languageCode) ? "rtl" : "ltr";

    private static readonly string[] GoogleFonts =
    {
        "Inter", "Poppins", "Roboto", "Open Sans", "Lato", "Merriweather", "Playfair Display", "Source Sans 3",
        "Noto Nastaliq Urdu", "Noto Naskh Arabic", "Gulzar", "Lateef", "Scheherazade New", "Amiri"
    };

    public static IReadOnlyCollection<string> FontNames => FontStacks.Keys.Where(IsAvailable).OrderBy(name => name).ToArray();
    public static IReadOnlyCollection<string> ThemeNames { get; } = new[] { "default", "dark", "editorial", "bold" };

    public static string NormalizeTheme(string? value) =>
        ThemeNames.FirstOrDefault(name => string.Equals(name, value, StringComparison.OrdinalIgnoreCase)) ?? "default";

    public static string NormalizeFont(string? value) =>
        FontStacks.Keys.FirstOrDefault(name => string.Equals(name, value, StringComparison.OrdinalIgnoreCase)) ?? "Inter";

    public static string GetGoogleFontStylesheetUrl(string? fontName)
    {
        var font = NormalizeFont(fontName);
        if (!GoogleFonts.Contains(font, StringComparer.OrdinalIgnoreCase)) return string.Empty;
        var family = Uri.EscapeDataString(font).Replace("%20", "+", StringComparison.Ordinal);
        // Urdu faces ship fewer weights; asking Google for a missing weight makes the whole request fail.
        var weights = font switch
        {
            "Noto Nastaliq Urdu" or "Noto Naskh Arabic" => "400;500;600;700",
            "Gulzar" => "400",
            "Lateef" => "400;700",
            "Scheherazade New" or "Amiri" => "400;700",
            _ => "400;500;600;700;800"
        };
        return $"https://fonts.googleapis.com/css2?family={family}:wght@{weights}&display=swap";
    }

    /// <summary>
    /// Stylesheet URL for the font the page actually renders with. On an Urdu site whose selected font has no
    /// Urdu glyphs (for example Inter) the Urdu font is used instead so text never falls back to a system font.
    /// </summary>
    public static string GetEffectiveFont(SiteProfile profile)
    {
        var font = NormalizeFont(profile.FontFamily);
        return IsRtl(profile.LanguageCode) && !UrduFonts.Contains(font, StringComparer.OrdinalIgnoreCase)
            ? "Noto Nastaliq Urdu"
            : font;
    }

    /// <summary>The selected font when it covers Urdu, otherwise Noto Nastaliq Urdu. Used for Urdu text in the admin editors.</summary>
    private static string GetUrduFont(SiteProfile profile)
    {
        var font = NormalizeFont(profile.FontFamily);
        return UrduFonts.Contains(font, StringComparer.OrdinalIgnoreCase) ? font : "Noto Nastaliq Urdu";
    }

    private static string BuildFontFaceCss(string fontName)
    {
        if (!LocalFonts.TryGetValue(fontName, out var local) || !IsAvailable(fontName)) return string.Empty;
        return $"@font-face{{font-family:'{fontName}';src:url('/fonts/{local.File}') format('{local.Format}');font-weight:100 900;font-style:normal;font-display:swap;}}";
    }

    public static string BuildCssVariables(SiteProfile profile, bool publicSite = true)
    {
        var theme = NormalizeTheme(profile.ThemeName);
        var font = FontStacks[publicSite ? GetEffectiveFont(profile) : NormalizeFont(profile.FontFamily)];
        var primary = SafeColor(profile.PrimaryColor, "#d30b12");
        var accent = SafeColor(profile.AccentColor, primary);
        var (background, surface, surfaceAlt, border, text, bodyText, muted, navBackground, navText) = theme switch
        {
            "dark" => ("#0d0d0d", "#1a1a1a", "#141414", "#2a2a2a", "#f0f0f0", "#c8c8c8", "#a0a0a0", "#0a0a0a", "rgba(240,240,240,.82)"),
            "editorial" => ("#fdf6e3", "#fffef8", "#f5eed6", "#e8dfc2", "#1a1008", "#2e2416", "#8a7a5a", "#1a1008", ""),
            "bold" => ("#f0f0f0", "#ffffff", "#e4e4e4", "#d0d0d0", "#0a0a0a", "#1a1a1a", "#666666", primary, "#ffffff"),
            _ => ("#f5f4f0", "#ffffff", "#f0ede6", "#e0dbd0", "#111111", "#2d2d2d", "#767676", "#111111", "rgba(255,255,255,.82)")
        };
        if (theme == "editorial") navText = "rgba(253,246,227,.82)";
        var urduFont = GetUrduFont(profile);
        var fontFaces = BuildFontFaceCss(urduFont) + BuildFontFaceCss(NormalizeFont(profile.FontFamily));
        var primaryRgb = ToRgb(primary);
        var accentRgb = ToRgb(accent);

        return fontFaces + $":root{{--bn-font-urdu:{FontStacks[urduFont]};--bn-bg:{background};--bn-surface:{surface};--bn-surface-alt:{surfaceAlt};--bn-border:{border};--bn-text-primary:{text};--bn-text-body:{bodyText};--bn-text-muted:{muted};--bn-brand-primary:{primary};--bn-brand-accent:{accent};--bn-brand-accent-hover:{Darken(accent)};--bn-accent:{primary};--bn-accent-hover:{Darken(primary)};--bn-nav-bg:{navBackground};--bn-nav-text:{navText};--bn-nav-text-hover:#ffffff;--bn-nav-active:{primary};--bn-ticker-bg:{accent};--bn-ticker-text:#ffffff;--bn-font-family:{font};--bn-font-body:{font};--bn-font-display:{font};--bn-font-ui:{font};--bn-font-accent:{font};--bs-body-font-family:var(--bn-font-family);--bs-body-bg:var(--bn-bg);--bs-body-color:var(--bn-text-primary);--bs-card-bg:var(--bn-surface);--bs-card-color:var(--bn-text-primary);--bs-border-color:var(--bn-border);--bs-primary-rgb:{primaryRgb};--bs-danger-rgb:{primaryRgb};--bs-link-color-rgb:{accentRgb};--bs-link-hover-color-rgb:{primaryRgb};}}";
    }

    private static string SafeColor(string? value, string fallback) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$") ? value : fallback;

    private static string ToRgb(string hex) => string.Join(",", Enumerable.Range(1, 3)
        .Select(index => Convert.ToInt32(hex.Substring(index * 2 - 1, 2), 16)));

    private static string Darken(string hex)
    {
        var channels = Enumerable.Range(1, 3).Select(index => Convert.ToInt32(hex.Substring(index * 2 - 1, 2), 16) * 0.82)
            .Select(value => ((int)value).ToString("x2"));
        return "#" + string.Concat(channels);
    }
}
