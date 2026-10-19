using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace BolNews.Application.Services;

public static class InternalLinkPhraseMatcher
{
    public static Regex CreateWholePhraseRegex(string phrase)
    {
        var words = Regex.Matches(phrase, @"[\p{L}\p{N}]+(?:['’][\p{L}\p{N}]+)?")
            .Select(match => Regex.Escape(match.Value));
        var phrasePattern = string.Join(@"[\s\p{P}]+", words);

        return new Regex(
            $@"(?<![\p{{L}}\p{{N}}_]){phrasePattern}(?![\p{{L}}\p{{N}}_])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    public static bool AppearsAsWholePhraseInUnlinkedText(string html, string phrase)
    {
        if (string.IsNullOrWhiteSpace(html) || string.IsNullOrWhiteSpace(phrase))
            return false;

        var regex = CreateWholePhraseRegex(phrase);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        return doc.DocumentNode.SelectNodes(
                "//text()[normalize-space(.) != '' and not(ancestor::a) and not(ancestor::script) " +
                "and not(ancestor::style) and not(ancestor::pre) and not(ancestor::code)]")
            ?.Any(node => regex.IsMatch(node.InnerText)) == true;
    }
}
