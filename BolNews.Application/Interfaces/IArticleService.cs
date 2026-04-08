using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;

namespace BolNews.Application.Interfaces
{
    public interface IArticleService
    {
        Task<int> CreateAsync(ArticleDto dto);
        Task UpdateAsync(ArticleDto dto);
        Task DeleteAsync(int id);

        Task<ArticleDto?> GetByIdAsync(int id);
        Task<IEnumerable<ArticleDto>> GetAllAsync();
    }
}
