using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces;

public interface IArticleInternalLinkRuleRepository
{
    Task<List<ArticleInternalLinkRule>> GetForSourceAsync(int sourceArticleId);
    Task AddAsync(ArticleInternalLinkRule rule);
    Task<bool> DeleteAsync(int sourceArticleId, int ruleId);
}
