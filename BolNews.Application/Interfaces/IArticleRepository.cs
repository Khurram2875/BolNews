using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;

namespace BolNews.Application.Interfaces
{
    public interface IArticleRepository
    {
        Task<int> AddAsync(Article article);
        Task UpdateAsync(Article article);
        Task<Article?> FindByIdAsync(int id);
        Task<Article?> FindPublishedByIdAsync(int id);
        Task<bool> SlugExistsAsync(string slug);
        Task<Article?> FindBySlugAsync(string slug);
        Task<PublicArticleData?> GetPublicArticleBySlugAsync(string slug);
        Task<List<Article>> GetAllAsync();
        Task<List<Article>> GetByAuthorIdAsync(int authorId, int page, int pageSize);
        Task<List<Article>> GetByCategorySlugAsync(string categorySlug, int page, int pageSize);
        Task<List<Article>> GetCategoryArticlesAsync(string categorySlug, int skip, int take);
        Task<List<Article>> GetByTagSlugAsync(string tagSlug, int page, int pageSize);
        Task<List<Article>> GetPublishedAsync(int count);
        Task<Article?> GetLatestPublishedAsync(IReadOnlyCollection<int>? excludedArticleIds = null);
        Task<List<Article>> GetLatestPublishedAsync(int count, IReadOnlyCollection<int>? excludedArticleIds = null);
        Task<List<Article>> GetByCategoryIdAsync(int categoryId, int count);
        Task<List<Article>> GetRelatedArticlesAsync(int articleId, int categoryId, IReadOnlyCollection<int> tagIds, int count);
       
        Task<List<Article>> GetForCategoriesAsync(List<int> categoryIds, int count);
        Task<List<Article>> GetPublishedSinceAsync(DateTime fromDate, int limit);
        Task<List<Article>> SearchAsync(string term, int page, int pageSize);
        Task<List<Article>> GetTrendingCandidatesAsync(DateTime fromDate, int candidateLimit);
        Task<List<Article>> GetTopByViewCountAsync(int count);
        Task<List<Article>> GetLowPerformingAsync(DateTime since, int maxViews, int limit);
        Task<int> CountAsync();
        Task<int> CountPublishedSinceAsync(DateTime since);
        Task<List<(DateTime Date, int Count)>> CountPerDayAsync(DateTime fromDate);
        Task<List<CategoryPerformanceDto>> GetCategoryPerformanceAsync(DateTime fromDate);
        Task<List<EditorPerformanceDto>> GetEditorPerformanceAsync(DateTime fromDate);
        Task IncrementViewCountAsync(int articleId);
        Task BulkUpdateAsync(IEnumerable<Article> articles);
        Task<List<Article>> GetTopRankedPublishedAsync(int count);
        Task<List<Article>> GetTopRankedByCategoryAsync(int categoryId, int count);
        Task<List<Article>> GetByWorkflowStatusAsync(ArticleWorkflowStatus status);
        Task<List<Article>> GetEditorialQueueAsync(params ArticleWorkflowStatus[] statuses);
        Task<List<Article>> GetActiveWorkflowArticlesAsync();
        Task SaveChangesAsync();
        Task<List<Article>> GetDueScheduledArticlesAsync(DateTime utcNow);
        Task<List<Article>> SearchPublishedAsync(string search, int take = 20);
        Task<List<Article>> GetDeletedAsync();
        Task<Dictionary<int, List<Article>>> GetLatestArticlesForCategoriesAsync(IReadOnlyCollection<int> categoryIds, int count);
        Task<Article?> GetBySourceAsync(string sourceSystem, string sourceId);
        Task<Article?> FindBySourceAsync(string sourceSystem,string sourceId);
        Task<List<Article>> GetWordPressArticlesWithMissingFeaturedImagesAsync();
        Task<(List<ArticleListDto> Articles, int TotalCount)> GetPagedAsync(
            string? authorUserId,
            int page,
            int pageSize,
            string? search = null,
            int? authorId = null);

        Task<(List<ArticleListDto> Articles, int TotalCount)> GetDeletedPagedAsync(
            int page,
            int pageSize);

    }
}
