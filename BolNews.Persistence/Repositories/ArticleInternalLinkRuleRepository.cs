using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Persistence.Repositories;

public class ArticleInternalLinkRuleRepository(AppDbContext context) : IArticleInternalLinkRuleRepository
{
    public Task<List<ArticleInternalLinkRule>> GetForSourceAsync(int sourceArticleId)
        => context.Set<ArticleInternalLinkRule>()
            .AsNoTracking()
            .Where(rule => rule.SourceArticleId == sourceArticleId)
            .Include(rule => rule.TargetArticle)
                .ThenInclude(article => article!.Category)
            .OrderBy(rule => rule.CreatedAt)
            .ThenBy(rule => rule.Id)
            .ToListAsync();

    public async Task AddAsync(ArticleInternalLinkRule rule)
    {
        context.Set<ArticleInternalLinkRule>().Add(rule);
        await context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int sourceArticleId, int ruleId)
    {
        var rule = await context.Set<ArticleInternalLinkRule>()
            .FirstOrDefaultAsync(rule => rule.Id == ruleId && rule.SourceArticleId == sourceArticleId);
        if (rule == null)
            return false;

        context.Remove(rule);
        await context.SaveChangesAsync();
        return true;
    }
}
