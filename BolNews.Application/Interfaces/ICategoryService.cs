using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;
using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task<CategoryDto?> GetByIdAsync(int id);
        Task<CategoryDto> CreateAsync(CategoryDto dto);
        Task<CategoryDto> UpdateAsync(CategoryDto dto);
        Task DeleteAsync(int id);
        Task<string> GetCategorySlug(int categoryId);
        Task<Category> GetBySlugAsync(string slug);
        Task<List<Category>> GetAllAsyncNew();
        Task<List<Category>> GetHomeCategoriesAsync();
    }
}
