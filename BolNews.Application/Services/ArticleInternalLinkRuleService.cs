using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;

namespace BolNews.Application.Services;

public class ArticleInternalLinkRuleService(
    IArticleInternalLinkRuleRepository repository,
    IArticleService articleService) : IArticleInternalLinkRuleService
{
    public Task<List<ArticleInternalLinkRule>> GetForSourceAsync(int sourceArticleId)
        => repository.GetForSourceAsync(sourceArticleId);

    public async Task AddExclusionAsync(int sourceArticleId, int targetArticleId)
    {
        if (sourceArticleId == targetArticleId)
            throw new InvalidOperationException("An article cannot exclude itself from internal linking.");

        var source = await articleService.GetEntityByIdAsync(sourceArticleId)
            ?? throw new InvalidOperationException("The source article could not be found.");
        var target = await articleService.GetEntityByIdAsync(targetArticleId)
            ?? throw new InvalidOperationException("The target article could not be found.");
        if (!target.IsPublished || target.WorkflowStatus != ArticleWorkflowStatus.Published)
            throw new InvalidOperationException("Only published articles can be excluded as link targets.");

        var existingRules = await repository.GetForSourceAsync(sourceArticleId);
        if (existingRules.Any(rule => rule.RuleType == ArticleInternalLinkRuleType.ExcludeAutomatic &&
                                      rule.TargetArticleId == targetArticleId))
            return;

        await repository.AddAsync(new ArticleInternalLinkRule
        {
            SourceArticleId = source.Id,
            TargetArticleId = target.Id,
            RuleType = ArticleInternalLinkRuleType.ExcludeAutomatic,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task AddManualLinkAsync(int sourceArticleId, string anchorText, int? targetArticleId, string? targetUrl)
    {
        anchorText = (anchorText ?? string.Empty).Trim();
        targetUrl = string.IsNullOrWhiteSpace(targetUrl) ? null : targetUrl.Trim();

        if (anchorText.Length is < 1 or > 500)
            throw new InvalidOperationException("Anchor text must be between 1 and 500 characters.");
        if ((targetArticleId.HasValue ? 1 : 0) + (targetUrl != null ? 1 : 0) != 1)
            throw new InvalidOperationException("Choose one destination: a published article or a URL.");
        if (targetUrl != null && !IsSafeDestinationUrl(targetUrl))
            throw new InvalidOperationException("Use an http(s) URL or a site-relative path beginning with /.");

        var source = await articleService.GetEntityByIdAsync(sourceArticleId)
            ?? throw new InvalidOperationException("The source article could not be found.");

        Article? target = null;
        if (targetArticleId.HasValue)
        {
            if (targetArticleId.Value == sourceArticleId)
                throw new InvalidOperationException("An article cannot link to itself.");

            target = await articleService.GetEntityByIdAsync(targetArticleId.Value)
                ?? throw new InvalidOperationException("The target article could not be found.");
            if (!target.IsPublished || target.WorkflowStatus != ArticleWorkflowStatus.Published)
                throw new InvalidOperationException("Only published articles can be used as link targets.");
        }

        if (!InternalLinkPhraseMatcher.AppearsAsWholePhraseInUnlinkedText(source.Content, anchorText))
            throw new InvalidOperationException("That complete word or phrase was not found in unlinked article text.");

        var existingRules = await repository.GetForSourceAsync(sourceArticleId);
        if (existingRules.Any(rule => rule.RuleType == ArticleInternalLinkRuleType.Manual &&
                                      string.Equals(rule.AnchorText, anchorText, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A manual link already uses that anchor text for this article.");

        await repository.AddAsync(new ArticleInternalLinkRule
        {
            SourceArticleId = source.Id,
            TargetArticleId = target?.Id,
            TargetUrl = targetUrl,
            AnchorText = anchorText,
            RuleType = ArticleInternalLinkRuleType.Manual,
            CreatedAt = DateTime.UtcNow
        });
    }

    public Task<bool> DeleteAsync(int sourceArticleId, int ruleId)
        => repository.DeleteAsync(sourceArticleId, ruleId);

    private static bool IsSafeDestinationUrl(string value)
    {
        if (value.Any(char.IsControl))
            return false;

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
            return absolute.Scheme is "http" or "https";

        return value.StartsWith("/", StringComparison.Ordinal) && !value.StartsWith("//", StringComparison.Ordinal);
    }
}
