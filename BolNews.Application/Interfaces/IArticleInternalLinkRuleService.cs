using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces;

public interface IArticleInternalLinkRuleService
{
    Task<List<ArticleInternalLinkRule>> GetForSourceAsync(int sourceArticleId);
    Task AddExclusionAsync(int sourceArticleId, int targetArticleId);
    Task AddManualLinkAsync(int sourceArticleId, string anchorText, int? targetArticleId, string? targetUrl);
    Task<bool> DeleteAsync(int sourceArticleId, int ruleId);
}
