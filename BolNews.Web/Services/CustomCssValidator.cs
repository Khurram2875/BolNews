using System.Text;
using System.Text.RegularExpressions;

namespace BolNews.Web.Services;

/// <summary>
/// Safety check for publisher CSS. It blocks constructs that can run script or pull in other code, and catches
/// unbalanced braces so one typo cannot swallow the whole stylesheet. The editor in Publisher Settings applies the same
/// rules while typing (tools/css-editor/src/editor.js); this class is the authoritative check on save and on serving.
/// </summary>
public static class CustomCssValidator
{
    public const int MaxLength = 100_000;

    private static readonly (Regex Pattern, string Message)[] Forbidden =
    {
        (new Regex(@"@import\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "@import is not allowed. Put the rules directly in the editor."),
        (new Regex(@"javascript\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled), "javascript: URLs are not allowed."),
        (new Regex(@"vbscript\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled), "vbscript: URLs are not allowed."),
        (new Regex(@"expression\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled), "expression() is not allowed."),
        (new Regex(@"behavior\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled), "behavior is not allowed."),
        (new Regex(@"-moz-binding", RegexOptions.IgnoreCase | RegexOptions.Compiled), "-moz-binding is not allowed."),
        (new Regex(@"data\s*:\s*text/html", RegexOptions.IgnoreCase | RegexOptions.Compiled), "data:text/html URLs are not allowed."),
        (new Regex(@"</|<!--", RegexOptions.Compiled), "HTML tags are not allowed in CSS (remove < characters).")
    };

    private static readonly Regex Comments = new(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>Returns human-readable problems; an empty list means the CSS is acceptable.</summary>
    public static List<string> Validate(string? css)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(css)) return problems;
        if (css.Length > MaxLength)
            problems.Add($"Custom CSS must be {MaxLength:N0} characters or fewer.");

        var plain = Comments.Replace(css, " ");
        if (plain.Contains("/*", StringComparison.Ordinal))
            problems.Add("A comment starting with /* is never closed with */.");

        foreach (var (pattern, message) in Forbidden)
            if (pattern.IsMatch(plain)) problems.Add(message);

        var depth = 0;
        char? quote = null;
        for (var i = 0; i < plain.Length; i++)
        {
            var c = plain[i];
            if (quote is not null)
            {
                if (c == '\\') i++;
                else if (c == quote) quote = null;
                continue;
            }
            if (c is '"' or '\'') { quote = c; continue; }
            if (c == '{') depth++;
            else if (c == '}' && --depth < 0)
            {
                problems.Add("There is a } without a matching {.");
                depth = 0;
            }
        }
        if (depth > 0) problems.Add("A { is never closed with }.");
        if (quote is not null) problems.Add("A quoted string is never closed.");
        return problems;
    }
}
