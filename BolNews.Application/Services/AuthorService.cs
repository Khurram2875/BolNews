using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Application.Services
{
    public class AuthorService : IAuthorService
    {
        private readonly AppDbContext _context;

        public AuthorService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AuthorDto>> GetAllAsync()
        {
            return await _context.Authors
                .Where(a => !a.IsDeleted)
                .Select(a => new AuthorDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Bio = a.Bio,
                    ProfileImageUrl = a.ProfileImageUrl,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    IsDeleted = a.IsDeleted
                })
                .ToListAsync();
        }

        public async Task<AuthorDto?> GetByIdAsync(int id)
        {
            return await _context.Authors
                .Where(a => a.Id == id && !a.IsDeleted)
                .Select(a => new AuthorDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Bio = a.Bio,
                    ProfileImageUrl = a.ProfileImageUrl,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    IsDeleted = a.IsDeleted
                })
                .FirstOrDefaultAsync();
        }

        public async Task<AuthorDto> CreateAsync(AuthorDto dto)
        {
            var entity = new Author
            {
                Name = dto.Name,
                Bio = dto.Bio,
                ProfileImageUrl = dto.ProfileImageUrl,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Authors.Add(entity);
            await _context.SaveChangesAsync();

            dto.Id = entity.Id;
            return dto;
        }

        public async Task<AuthorDto> UpdateAsync(AuthorDto dto)
        {
            var entity = await _context.Authors.FindAsync(dto.Id);
            if (entity == null || entity.IsDeleted)
                throw new KeyNotFoundException("Author not found");

            entity.Name = dto.Name;
            entity.Bio = dto.Bio;
            entity.ProfileImageUrl = dto.ProfileImageUrl;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return dto;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Authors.FindAsync(id);
            if (entity == null || entity.IsDeleted)
                throw new KeyNotFoundException("Author not found");

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
        public async Task UpdateImageAsync(int id, string imagePath)
        {
            var entity = await _context.Authors.FindAsync(id);
            if (entity != null)
            {
                entity.ProfileImageUrl = imagePath;
                await _context.SaveChangesAsync();
            }
        }
    }
}
