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
        Task<bool> SlugExistsAsync(string slug);
        Task<Article?> FindBySlugAsync(string slug);
        Task<List<Article>> GetAllAsync();
        Task<List<Article>> GetByAuthorIdAsync(int authorId, int page, int pageSize);
        Task<List<Article>> GetByCategorySlugAsync(string categorySlug, int page, int pageSize);
        Task<List<Article>> GetPublishedAsync(int count);
        Task<List<Article>> GetByCategoryIdAsync(int categoryId, int count);
        Task<List<Article>> GetForCategoriesAsync(List<int> categoryIds);
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
    }
}
