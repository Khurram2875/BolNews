using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;
using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface IArticleService
    {
        Task<int> CreateAsync(ArticleDto dto);
        Task UpdateAsync(ArticleDto dto, string currentUserId, IList<string> roles, string? changeReason = null);
        Task DeleteAsync(int id);

        Task<ArticleDto?> GetByIdAsync(int id);
        Task<IEnumerable<ArticleDto>> GetAllAsync(string Id, IList<string> roles);
        Task UpdateImagesAsync(int articleId, string thumb, string medium, string large, string xl);
        Task<string> GenerateUniqueSlugAsync(string title);
        Task<Article> GetBySlugAsync(string slug);
        Task<List<Article>> GetByCategorySlugAsync(string categorySlug, int page);
        Task<List<Article>> GetRelatedArticlesAsync(int categoryId, int excludeArticleId, int count = 5);
        Task<List<Article>> GetLatestArticlesAsync(int count = 8);
        Task<List<Article>> GetAllPublishedAsync();
        Task<Article?> GetTopStoryAsync();
        Task<List<Article>> GetSecondaryStoriesAsync(int count = 4);
        Task<List<Article>> GetArticlesByCategoryAsync(int categoryId, int count = 5);
        Task<Dictionary<int, List<Article>>> GetArticlesForCategoriesAsync(List<int> categoryIds, int count);
        Task<List<Article>> GetBreakingNewsAsync(int count = 5);
        Task<List<Article>> SearchAsync(string query, int page, int pageSize);
        Task IncrementViewCountAsync(int articleId);
        //Task<List<Article>> GetTrendingAsync(int count = 5);
        Task<List<Article>> GetTrendingAsync(int count = 5, string type = "week");
        Task<List<Article>> GetLatestPublishedAsync(DateTime fromDate, int limit);
        Task<List<Article>> GetRecentArticlesAsync(int hours = 48);
        Task<int> GetTotalArticlesAsync();
        Task<int> GetTodayArticlesCountAsync();
        Task<List<Article>> GetTopArticlesAsync(int count = 10);
        Task<List<Article>> GetLowPerformingArticlesAsync();
        Task<List<(DateTime date, int count)>> GetArticlesPerDayAsync(int days = 7);
        Task<List<CategoryPerformanceDto>> GetCategoryPerformanceAsync(int days = 7);
        Task<List<EditorPerformanceDto>> GetEditorPerformanceAsync(int days = 7);
        Task<bool> CanEditAsync(int articleId, string userId, IList<string> roles);
        Task<bool> CanDeleteAsync(int articleId, string userId, IList<string> roles);
        Task<List<Article>> GetByAuthorAsync(int authorId, int page = 1, int pageSize = 20);
        Task<IEnumerable<ArticleDto>> GetTopRankedPublishedAsync(int count);
        Task<IEnumerable<ArticleDto>> GetTopRankedByCategoryAsync(int categoryId, int count);

    }
}
