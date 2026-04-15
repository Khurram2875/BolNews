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
    }
}
