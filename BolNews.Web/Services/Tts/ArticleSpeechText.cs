using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace BolNews.Web.Services.Tts;

/// <summary>Turns an article (title, summary, HTML body) into plain text for a voice: embeds, scripts and ads are dropped, and every paragraph ends with sentence punctuation so the voice pauses.</summary>
public static class ArticleSpeechText
{
    private static readonly HashSet<string> Skipped = new(StringComparer.OrdinalIgnoreCase)
        { "script", "style", "iframe", "video", "audio", "object", "embed", "noscript", "button", "form", "svg", "img", "picture", "figure" };
    private static readonly HashSet<string> Blocks = new(StringComparer.OrdinalIgnoreCase)
        { "p", "div", "h1", "h2", "h3", "h4", "h5", "h6", "li", "ul", "ol", "blockquote", "br", "tr", "section", "article" };
    private static readonly Regex Terminal = new("[.!?\u06D4\u061F\u2026:;\"'\u201D\u2019)]$", RegexOptions.Compiled);

    public static string Build(string? title, string? summary, string? contentHtml, bool urdu)
    {
        var stop = urdu ? "\u06D4" : ".";
        var lines = new List<string>();
        AddLine(lines, title, stop);
        AddLine(lines, summary, stop);

        if (!string.IsNullOrWhiteSpace(contentHtml))
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(contentHtml);
            var sb = new StringBuilder();
            Walk(doc.DocumentNode, sb);
            foreach (var line in sb.ToString().Split('\n')) AddLine(lines, line, stop);
        }
        return string.Join("\n", lines);
    }

    private static void AddLine(List<string> lines, string? raw, string stop)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;
        var text = Regex.Replace(HtmlEntity.DeEntitize(raw), "\\s+", " ").Trim();
        if (!Regex.IsMatch(text, "[\\p{L}\\p{N}]")) return;
        lines.Add(Terminal.IsMatch(text) ? text : text + stop);
    }

    private static void Walk(HtmlNode node, StringBuilder sb)
    {
        foreach (var child in node.ChildNodes)
        {
            switch (child.NodeType)
            {
                case HtmlNodeType.Text:
                    sb.Append(child.InnerText);
                    break;
                case HtmlNodeType.Element:
                    if (Skipped.Contains(child.Name)) break;
                    var cls = child.GetAttributeValue("class", "");
                    if (cls.Contains("bn-ad", StringComparison.OrdinalIgnoreCase) || cls.Contains("twitter-tweet", StringComparison.OrdinalIgnoreCase)) break;
                    var block = Blocks.Contains(child.Name);
                    if (block) sb.Append('\n');
                    Walk(child, sb);
                    if (block) sb.Append('\n');
                    break;
            }
        }
    }
}
