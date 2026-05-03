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
                    Id             = a.Id,
                    Name           = a.Name,
                    Bio            = a.Bio,
                    Slug           = a.Slug,
                    ProfileImageUrl = a.ProfileImageUrl,
                    CreatedAt      = a.CreatedAt,
                    UpdatedAt      = a.UpdatedAt,
                    IsDeleted      = a.IsDeleted
                })
                .ToListAsync();
        }

        public async Task<List<AuthorDto>> GetAll()
        {
            return await _context.Authors
                .Include(a => a.Articles)
                .Where(a => !a.IsDeleted)
                .Select(a => new AuthorDto
                {
                    Id             = a.Id,
                    Name           = a.Name,
                    Bio            = a.Bio,
                    Slug           = a.Slug,
                    ProfileImageUrl = a.ProfileImageUrl,
                    Articles       = a.Articles,
                    CreatedAt      = a.CreatedAt,
                    UpdatedAt      = a.UpdatedAt,
                    IsDeleted      = a.IsDeleted
                })
                .ToListAsync();
        }

        public async Task<AuthorDto?> GetByIdAsync(int id)
        {
            return await _context.Authors
                .Where(a => a.Id == id && !a.IsDeleted)
                .Select(a => new AuthorDto
                {
                    Id             = a.Id,
                    Name           = a.Name,
                    Bio            = a.Bio,
                    Slug           = a.Slug,
                    ProfileImageUrl = a.ProfileImageUrl,
                    CreatedAt      = a.CreatedAt,
                    UpdatedAt      = a.UpdatedAt,
                    IsDeleted      = a.IsDeleted,
                    UserId         = a.UserId
                })
                .FirstOrDefaultAsync();
        }

        public async Task<AuthorDto> CreateAsync(AuthorDto dto)
        {
            var entity = new Author
            {
                Name           = dto.Name,
                Bio            = dto.Bio,
                ProfileImageUrl = dto.ProfileImageUrl,
                CreatedAt      = DateTime.UtcNow,
                IsDeleted      = false,
                UserId         = dto.UserId,
                Slug           = NormalizeName(dto.Name)
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

            entity.Name           = dto.Name;
            entity.Bio            = dto.Bio;
            entity.ProfileImageUrl = dto.ProfileImageUrl;
            entity.UpdatedAt      = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return dto;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Authors.FindAsync(id);
            if (entity == null || entity.IsDeleted)
                throw new KeyNotFoundException("Author not found");

            entity.IsDeleted  = true;
            entity.UpdatedAt  = DateTime.UtcNow;

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

        public async Task<AuthorDto?> GetAuthorByUserId(string userId)
        {
            return await _context.Authors
                .Where(a => a.UserId == userId && !a.IsDeleted)
                .Select(a => new AuthorDto
                {
                    Id             = a.Id,
                    Name           = a.Name,
                    Bio            = a.Bio,
                    Slug           = a.Slug,
                    ProfileImageUrl = a.ProfileImageUrl,
                    CreatedAt      = a.CreatedAt,
                    UpdatedAt      = a.UpdatedAt,
                    IsDeleted      = a.IsDeleted
                })
                .FirstOrDefaultAsync();
        }

        public async Task<Author?> GetBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            return await _context.Authors
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Slug == slug);
        }

        private static string NormalizeName(string name)
            => name.Replace("-", " ").Trim().ToLower();
    }
}
