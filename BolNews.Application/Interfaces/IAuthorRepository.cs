using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface IAuthorRepository
    {
        Task<List<Author>> GetAllAsync(bool includeArticles = false);
        Task<Author?> FindByIdAsync(int id);
        Task<Author?> FindByUserIdAsync(string userId);
        Task<Author?> FindBySlugAsync(string slug);
        Task AddAsync(Author author);
        Task UpdateAsync(Author author);
    }
}
