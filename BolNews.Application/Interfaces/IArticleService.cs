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
        Task UpdateAsync(ArticleDto dto);
        Task DeleteAsync(int id);

        Task<ArticleDto?> GetByIdAsync(int id);
        Task<IEnumerable<ArticleDto>> GetAllAsync();
        Task UpdateImagesAsync(int articleId, string thumb, string medium, string large);
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
    }
}
