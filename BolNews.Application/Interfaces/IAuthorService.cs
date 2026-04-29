using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;
using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface IAuthorService
    {
        Task<IEnumerable<AuthorDto>> GetAllAsync();
        Task<AuthorDto?> GetByIdAsync(int id);
        Task<AuthorDto?> GetAuthorByUserId(string userId);
        Task<AuthorDto> CreateAsync(AuthorDto dto);
        Task<AuthorDto> UpdateAsync(AuthorDto dto);
        Task DeleteAsync(int id);
        Task UpdateImageAsync(int id, string imagePath);
        Task<Author?> GetBySlugAsync(string slug);
        Task<List<AuthorDto2>> GetAll();
    }
}
