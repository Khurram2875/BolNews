using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace BolNews.Web.Helpers;

/// <summary>
/// Sanitises the Quill HTML that editors write for Live Events (intro box and update details)
/// and renders legacy plain-text updates safely. Anything not on the allow-list is removed.
/// Social embeds survive as <c>div.quill-social-embed</c> carrying their base64 payload in
/// <c>data-raw-html</c>; the page script restores them exactly like the article page does.
/// </summary>
public static class LiveHtml
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "h1", "h2", "h3", "h4", "h5", "h6", "strong", "b", "em", "i", "u", "s", "strike",
        "ul", "ol", "li", "blockquote", "pre", "code", "a", "img", "span", "div", "sub", "sup", "hr"
    };

    // Elements removed together with everything inside them.
    private static readonly HashSet<string> DroppedWithContent = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "iframe", "object", "embed", "form", "input", "button", "textarea", "select",
        "link", "meta", "base", "svg", "math", "template", "noscript", "audio", "video", "frame", "frameset"
    };

    private static readonly Regex ClassToken = new(@"^(ql-[a-z0-9-]+|quill-social-embed)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SafeStyle = new(
        @"^\s*(color|background-color)\s*:\s*(#[0-9a-f]{3,8}|rgba?\(\s*\d{1,3}\s*(,\s*\d{1,3}\s*){2}(,\s*[\d.]+\s*)?\)|[a-z]{3,20})\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Base64Payload = new(@"^[A-Za-z0-9+/=]{1,200000}$", RegexOptions.Compiled);
    private static readonly Regex LooksLikeHtml = new(@"^\s*<(p|h[1-6]|ul|ol|div|blockquote|pre)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        CleanChildren(doc.DocumentNode);
        return doc.DocumentNode.InnerHtml.Trim();
    }

    /// <summary>True when the HTML shows anything to a reader: text, an image, or a social embed.</summary>
    public static bool HasContent(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return false;
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        if (doc.DocumentNode.SelectSingleNode("//img | //iframe | //*[contains(@class,'quill-social-embed')]") != null) return true;
        return !string.IsNullOrWhiteSpace(WebUtility.HtmlDecode(doc.DocumentNode.InnerText).Replace(' ', ' '));
    }

    /// <summary>HTML for display: Quill HTML is sanitised again; legacy plain text is encoded with line breaks.</summary>
    public static string Render(string? details)
    {
        if (string.IsNullOrWhiteSpace(details)) return string.Empty;
        if (LooksLikeHtml.IsMatch(details)) return Sanitize(details);
        return WebUtility.HtmlEncode(details).Replace("\r\n", "\n").Replace("\n", "<br />");
    }

    /// <summary>Loads stored details into the editor: Quill HTML as-is, legacy plain text as paragraphs.</summary>
    public static string ToEditorHtml(string? details)
    {
        if (string.IsNullOrWhiteSpace(details)) return string.Empty;
        if (LooksLikeHtml.IsMatch(details)) return details;
        var lines = details.Replace("\r\n", "\n").Split('\n');
        return string.Concat(lines.Select(l => string.IsNullOrWhiteSpace(l) ? "<p><br></p>" : "<p>" + WebUtility.HtmlEncode(l) + "</p>"));
    }

    private static void CleanChildren(HtmlNode parent)
    {
        foreach (var node in parent.ChildNodes.ToList())
        {
            if (node.NodeType == HtmlNodeType.Comment) { node.Remove(); continue; }
            if (node.NodeType != HtmlNodeType.Element) continue;

            if (DroppedWithContent.Contains(node.Name)) { node.Remove(); continue; }

            if (!AllowedTags.Contains(node.Name))
            {
                // Unknown wrapper: keep its (cleaned) children, drop the tag itself.
                CleanChildren(node);
                parent.RemoveChild(node, keepGrandChildren: true);
                continue;
            }

            CleanAttributes(node);

            if (node.Name.Equals("img", StringComparison.OrdinalIgnoreCase) && !node.Attributes.Contains("src"))
            {
                node.Remove();
                continue;
            }

            if (node.HasClass("quill-social-embed"))
            {
                // Content is restored client-side from the validated payload; never keep markup inside.
                node.RemoveAllChildren();
                continue;
            }

            CleanChildren(node);
        }
    }

    private static void CleanAttributes(HtmlNode node)
    {
        var name = node.Name.ToLowerInvariant();
        var isEmbed = name == "div" && node.GetAttributeValue("class", "").Split(' ').Contains("quill-social-embed");

        foreach (var attr in node.Attributes.ToList())
        {
            var key = attr.Name.ToLowerInvariant();
            var value = attr.Value ?? string.Empty;
            var keep = false;

            switch (key)
            {
                case "class":
                    var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => ClassToken.IsMatch(t)).ToArray();
                    if (tokens.Length > 0) { attr.Value = string.Join(' ', tokens); keep = true; }
                    break;
                case "style":
                    var decls = value.Split(';', StringSplitOptions.RemoveEmptyEntries).Where(d => SafeStyle.IsMatch(d)).ToArray();
                    if (decls.Length > 0) { attr.Value = string.Join("; ", decls.Select(d => d.Trim())); keep = true; }
                    break;
                case "href" when name == "a":
                    keep = IsSafeUrl(value, allowMailto: true);
                    break;
                case "src" when name == "img":
                    keep = IsSafeUrl(value, allowMailto: false);
                    break;
                case "target" when name == "a":
                case "alt" when name == "img":
                case "width" when name == "img":
                case "height" when name == "img":
                case "data-size" when name == "img":
                    keep = true;
                    break;
                case "data-raw-html" when isEmbed:
                    keep = Base64Payload.IsMatch(value);
                    break;
                case "contenteditable" when isEmbed:
                    attr.Value = "false";
                    keep = true;
                    break;
            }

            if (!keep) node.Attributes.Remove(attr);
        }

        if (name == "a")
        {
            node.SetAttributeValue("rel", "noopener noreferrer");
            if (node.Attributes.Contains("target")) node.SetAttributeValue("target", "_blank");
        }
    }

    private static bool IsSafeUrl(string value, bool allowMailto)
    {
        var v = value.Trim();
        if (v.Length == 0) return false;
        if (v.StartsWith("//")) return true;
        if (v.StartsWith("/") || v.StartsWith("#")) return true;
        return v.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || v.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || (allowMailto && v.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase));
    }
}
