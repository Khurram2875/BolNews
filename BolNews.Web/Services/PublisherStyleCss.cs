using System.Globalization;
using System.Text;
using BolNews.Domain.Entities;

namespace BolNews.Web.Services;

/// <summary>Turns validated <see cref="StyleSettings"/> into CSS. Only numbers and #RRGGBB values ever reach the output.</summary>
public static class PublisherStyleCss
{
    private static string N(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    private static bool Hex(string? v) => v is { Length: 7 } && v[0] == '#' && v.Skip(1).All(Uri.IsHexDigit);

    private static void Rule(StringBuilder sb, string selector, ElementStyle? e)
    {
        if (e is null || e.IsEmpty) return;
        var body = new StringBuilder();
        if (e.SizePx is >= 10 and <= 80) body.Append($"font-size:{e.SizePx}px!important;");
        if (e.Weight is >= 100 and <= 900 && e.Weight % 100 == 0) body.Append($"font-weight:{e.Weight}!important;");
        if (Hex(e.Color)) body.Append($"color:{e.Color}!important;");
        if (e.LineHeight is >= 1m and <= 3.5m) body.Append($"line-height:{N(e.LineHeight.Value)}!important;");
        if (body.Length > 0) sb.Append(selector).Append('{').Append(body).Append('}');
    }

    public static string Build(StyleSettings? style)
    {
        if (style is null) return string.Empty;
        var sb = new StringBuilder();
        Rule(sb, "html h1[itemprop=\"headline\"]", style.ArticleHeadline);
        Rule(sb, "html .bn-article-body h1", style.BodyH1);
        Rule(sb, "html .bn-article-body h2", style.BodyH2);
        Rule(sb, "html .bn-article-body h3", style.BodyH3);
        Rule(sb, "html .bn-article-body h4", style.BodyH4);
        Rule(sb, "html .bn-section-title", style.SectionTitle);
        if (style.ArticleLineHeight is >= 1m and <= 3.5m)
            sb.Append($"html .bn-article-body,html .bn-article-body p,html .bn-article-body li{{line-height:{N(style.ArticleLineHeight.Value)}!important;}}");
        if (Hex(style.ArticleLinkColor))
            sb.Append($"html .bn-article-body a{{color:{style.ArticleLinkColor}!important;}}");
        if (style.CornerRadiusPx is >= 0 and <= 32)
        {
            var r = style.CornerRadiusPx.Value;
            sb.Append($"html:root{{--bn-radius-sm:{r / 2}px;--bn-radius-md:{r}px;--bn-radius-lg:{r * 3 / 2}px;}}");
        }
        return sb.ToString();
    }
}
