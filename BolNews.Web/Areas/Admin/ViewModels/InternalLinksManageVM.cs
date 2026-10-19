using BolNews.Domain.Enums;

namespace BolNews.Web.Areas.Admin.ViewModels;

public class InternalLinksManageVM
{
    public int? SourceArticleId { get; set; }
    public string? SourceArticleTitle { get; set; }
    public List<InternalLinkRuleRowVM> Rules { get; set; } = new();
}

public class InternalLinkRuleRowVM
{
    public int Id { get; set; }
    public ArticleInternalLinkRuleType RuleType { get; set; }
    public string? AnchorText { get; set; }
    public string DestinationLabel { get; set; } = string.Empty;
    public string? DestinationUrl { get; set; }
}

public class AddManualInternalLinkVM
{
    public int SourceArticleId { get; set; }
    public string AnchorText { get; set; } = string.Empty;
    public int? TargetArticleId { get; set; }
    public string? TargetUrl { get; set; }
}

public class AddInternalLinkExclusionVM
{
    public int SourceArticleId { get; set; }
    public int TargetArticleId { get; set; }
}
