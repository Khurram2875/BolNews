using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync(bool includeDeleted = false);
        Task<List<Category>> GetParentsAsync();
        Task<List<Category>> GetParentsWithChildrenAsync();
        Task<Category?> FindByIdAsync(int id);
        Task<Category?> FindBySlugAsync(string slug);
        Task<string?> GetSlugByIdAsync(int categoryId);
        Task AddAsync(Category category);
        Task UpdateAsync(Category category);
    }
}
