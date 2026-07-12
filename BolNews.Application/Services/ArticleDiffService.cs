using System.Net;
using System.Text.RegularExpressions;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;

namespace BolNews.Application.Services
{
    public class ArticleDiffService : IArticleDiffService
    {
        public ArticleRevisionCompareDto Compare(
            ArticleRevisionDto fromRevision,
            ArticleDto toArticle,
            string toLabel)
        {
            var currentRevision =
                new ArticleRevisionDto
                {
                    Title = toArticle.Title,
                    Slug = toArticle.Slug,
                    MetaTitle = toArticle.MetaTitle,
                    MetaDescription = toArticle.MetaDescription,
                    Summary = toArticle.Summary,
                    Content = toArticle.Content,
                    FeaturedImageLarge = toArticle.FeaturedImageLarge,
                    FeaturedImageXl = toArticle.FeaturedImageXl,
                    IsPublishedSnapshot = toArticle.IsPublished
                };

            return Compare(
                fromRevision,
                currentRevision,
                toLabel);
        }

        public ArticleRevisionCompareDto Compare(
            ArticleRevisionDto fromRevision,
            ArticleRevisionDto toRevision,
            string toLabel)
        {
            var result =
                new ArticleRevisionCompareDto
                {
                    FromRevision = fromRevision,
                    ToLabel = toLabel
                };

            result.FieldDiffs.Add(
                BuildTextDiff("Title", fromRevision.Title, toRevision.Title));

            result.FieldDiffs.Add(
                BuildTextDiff("Slug", fromRevision.Slug, toRevision.Slug));

            result.FieldDiffs.Add(
                BuildTextDiff("Meta Title", fromRevision.MetaTitle, toRevision.MetaTitle));

            result.FieldDiffs.Add(
                BuildTextDiff("Meta Description", fromRevision.MetaDescription, toRevision.MetaDescription));

            result.FieldDiffs.Add(
                BuildTextDiff("Summary", fromRevision.Summary, toRevision.Summary));

            result.FieldDiffs.Add(
                BuildTextDiff(
                    "Content",
                    NormalizeHtmlText(fromRevision.Content),
                    NormalizeHtmlText(toRevision.Content)));

            result.FieldDiffs.Add(
                BuildTextDiff(
                    "Featured Image",
                    fromRevision.FeaturedImageLarge ?? fromRevision.FeaturedImageXl,
                    toRevision.FeaturedImageLarge ?? toRevision.FeaturedImageXl));

            result.FieldDiffs.Add(
                BuildTextDiff(
                    "Published",
                    fromRevision.IsPublishedSnapshot ? "Yes" : "No",
                    toRevision.IsPublishedSnapshot ? "Yes" : "No"));

            return result;
        }

        private static ArticleFieldDiffDto BuildTextDiff(
            string fieldName,
            string? oldValue,
            string? newValue)
        {
            var oldText =
                NormalizeText(oldValue);

            var newText =
                NormalizeText(newValue);

            var hasChanged =
                !string.Equals(
                    oldText,
                    newText,
                    StringComparison.Ordinal);

            return new ArticleFieldDiffDto
            {
                FieldName = fieldName,
                OldValue = oldText,
                NewValue = newText,
                HasChanged = hasChanged,
                DiffHtml = hasChanged
                    ? BuildWordDiff(oldText, newText)
                    : WebUtility.HtmlEncode(newText)
            };
        }

        private static string BuildWordDiff(
            string oldText,
            string newText)
        {
            var oldWords =
                Tokenize(oldText);

            var newWords =
                Tokenize(newText);

            var table =
                BuildLongestCommonSubsequenceTable(
                    oldWords,
                    newWords);

            var parts =
                new List<string>();

            var oldIndex = 0;
            var newIndex = 0;

            while (
                oldIndex < oldWords.Length &&
                newIndex < newWords.Length)
            {
                if (oldWords[oldIndex] == newWords[newIndex])
                {
                    parts.Add(
                        WebUtility.HtmlEncode(
                            newWords[newIndex]));

                    oldIndex++;
                    newIndex++;
                }
                else if (table[oldIndex + 1, newIndex] >= table[oldIndex, newIndex + 1])
                {
                    parts.Add(
                        $"<del>{WebUtility.HtmlEncode(oldWords[oldIndex])}</del>");

                    oldIndex++;
                }
                else
                {
                    parts.Add(
                        $"<ins>{WebUtility.HtmlEncode(newWords[newIndex])}</ins>");

                    newIndex++;
                }
            }

            while (oldIndex < oldWords.Length)
            {
                parts.Add(
                    $"<del>{WebUtility.HtmlEncode(oldWords[oldIndex])}</del>");

                oldIndex++;
            }

            while (newIndex < newWords.Length)
            {
                parts.Add(
                    $"<ins>{WebUtility.HtmlEncode(newWords[newIndex])}</ins>");

                newIndex++;
            }

            return string.Join(" ", parts);
        }

        private static int[,] BuildLongestCommonSubsequenceTable(
            string[] oldWords,
            string[] newWords)
        {
            var table =
                new int[oldWords.Length + 1, newWords.Length + 1];

            for (var oldIndex = oldWords.Length - 1; oldIndex >= 0; oldIndex--)
            {
                for (var newIndex = newWords.Length - 1; newIndex >= 0; newIndex--)
                {
                    table[oldIndex, newIndex] =
                        oldWords[oldIndex] == newWords[newIndex]
                            ? table[oldIndex + 1, newIndex + 1] + 1
                            : Math.Max(
                                table[oldIndex + 1, newIndex],
                                table[oldIndex, newIndex + 1]);
                }
            }

            return table;
        }

        private static string[] Tokenize(
            string text)
        {
            return text
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);
        }

        private static string NormalizeHtmlText(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var withoutTags =
                Regex.Replace(
                    value,
                    "<.*?>",
                    " ");

            return WebUtility.HtmlDecode(
                NormalizeText(withoutTags));
        }

        private static string NormalizeText(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return Regex.Replace(
                value,
                @"\s+",
                " ")
                .Trim();
        }
    }
}
