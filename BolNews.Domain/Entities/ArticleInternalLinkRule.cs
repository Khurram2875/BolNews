using BolNews.Domain.Enums;

namespace BolNews.Domain.Entities;

public class ArticleInternalLinkRule
{
    public int Id { get; set; }

    public int SourceArticleId { get; set; }
    public Article SourceArticle { get; set; } = null!;

    public int? TargetArticleId { get; set; }
    public Article? TargetArticle { get; set; }

    public ArticleInternalLinkRuleType RuleType { get; set; }

    public string? AnchorText { get; set; }
    public string? TargetUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}
