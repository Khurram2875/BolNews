namespace BolNews.Application.DTOs
{
    public class ArticleRevisionCompareDto
    {
        public ArticleRevisionDto FromRevision { get; set; } = new();

        public string ToLabel { get; set; } = string.Empty;

        public List<ArticleFieldDiffDto> FieldDiffs { get; set; } = new();

        public bool HasChanges =>
            FieldDiffs.Any(x => x.HasChanged);
    }

    public class ArticleFieldDiffDto
    {
        public string FieldName { get; set; } = string.Empty;

        public string OldValue { get; set; } = string.Empty;

        public string NewValue { get; set; } = string.Empty;

        public bool HasChanged { get; set; }

        public string DiffHtml { get; set; } = string.Empty;
    }
}
