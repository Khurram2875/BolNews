using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;
using HtmlAgilityPack;
using System.Net;

namespace BolNews.Application.Services
{
    public class InternalLinkingService : IInternalLinkingService
    {
        private readonly IArticleInternalLinkRuleService _linkRuleService;
        private readonly IUrlService _urlService;

        public InternalLinkingService(
            IArticleInternalLinkRuleService linkRuleService,
            IUrlService urlService)
        {
            _linkRuleService = linkRuleService;
            _urlService = urlService;
        }

        // Only explicit per-article manual rules create links. Automatic article
        // discovery and keyword-based link generation are intentionally disabled.
        public async Task<string> InjectInternalLinksAsync(string content, int currentArticleId)
        {
            if (string.IsNullOrWhiteSpace(content))
                return content;

            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var baseUrl = _urlService.GetBaseUrl();
            var rules = await _linkRuleService.GetForSourceAsync(currentArticleId);
            var textNodes = GetEligibleTextNodes(doc);
            var existingLinks = doc.DocumentNode
                .SelectNodes("//a[@href]")
                ?.Select(a => NormalizeHref(a.GetAttributeValue("href", ""), baseUrl))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ApplyManualLinks(doc, textNodes, rules, baseUrl, existingLinks);
            return doc.DocumentNode.InnerHtml;
        }

        private static List<HtmlNode> GetEligibleTextNodes(HtmlDocument doc)
            => doc.DocumentNode.SelectNodes(
                    "//text()[normalize-space(.) != '' " +
                    "and not(ancestor::a) " +
                    "and not(ancestor::script) " +
                    "and not(ancestor::style) " +
                    "and not(ancestor::pre) " +
                    "and not(ancestor::code)]")
                ?.ToList() ?? new List<HtmlNode>();

        private static void ApplyManualLinks(
            HtmlDocument doc,
            List<HtmlNode> textNodes,
            List<ArticleInternalLinkRule> rules,
            string baseUrl,
            HashSet<string> existingLinks)
        {
            foreach (var rule in rules.Where(rule => rule.RuleType == ArticleInternalLinkRuleType.Manual))
            {
                if (string.IsNullOrWhiteSpace(rule.AnchorText))
                    continue;

                var destination = rule.TargetArticle != null &&
                                  !string.IsNullOrWhiteSpace(rule.TargetArticle.Category?.Slug) &&
                                  !string.IsNullOrWhiteSpace(rule.TargetArticle.Slug)
                    ? $"{baseUrl}/{rule.TargetArticle.Category.Slug}/{rule.TargetArticle.Slug}"
                    : rule.TargetUrl;
                if (string.IsNullOrWhiteSpace(destination) || !IsSafeDestinationUrl(destination))
                    continue;

                var normalizedDestination = NormalizeHref(destination, baseUrl);
                if (existingLinks.Contains(normalizedDestination))
                    continue;

                var regex = InternalLinkPhraseMatcher.CreateWholePhraseRegex(rule.AnchorText);
                foreach (var node in textNodes.ToList())
                {
                    if (node.ParentNode == null)
                        continue;

                    var text = node.InnerText;
                    var match = regex.Match(text);
                    if (!match.Success)
                        continue;

                    var parent = node.ParentNode;
                    var anchor = doc.CreateElement("a");
                    anchor.SetAttributeValue("href", WebUtility.HtmlEncode(destination));
                    anchor.AppendChild(doc.CreateTextNode(text.Substring(match.Index, match.Length)));

                    var before = text.Substring(0, match.Index);
                    var after = text.Substring(match.Index + match.Length);
                    if (before.Length > 0) parent.InsertBefore(doc.CreateTextNode(before), node);
                    parent.InsertBefore(anchor, node);
                    if (after.Length > 0) parent.InsertBefore(doc.CreateTextNode(after), node);
                    parent.RemoveChild(node);
                    textNodes.Remove(node);
                    existingLinks.Add(normalizedDestination);
                    break;
                }
            }
        }

        private static bool IsSafeDestinationUrl(string value)
        {
            if (value.Any(char.IsControl))
                return false;
            if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
                return absolute.Scheme is "http" or "https";
            return value.StartsWith("/", StringComparison.Ordinal) && !value.StartsWith("//", StringComparison.Ordinal);
        }

        private static string NormalizeHref(string href, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(href)) return string.Empty;
            if (Uri.TryCreate(href, UriKind.Absolute, out var absolute))
            {
                if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) &&
                    string.Equals(absolute.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase))
                    return absolute.AbsolutePath.TrimEnd('/');
                return $"{absolute.GetLeftPart(UriPartial.Authority)}{absolute.AbsolutePath.TrimEnd('/')}";
            }
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var relativeBaseUri) &&
                Uri.TryCreate(relativeBaseUri, href, out var resolved))
                return resolved.AbsolutePath.TrimEnd('/');
            return (href.StartsWith('/') ? href : $"/{href.TrimStart('/')}").TrimEnd('/');
        }
    }
}
