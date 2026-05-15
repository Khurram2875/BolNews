using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services
{
    public class AuthorService : IAuthorService
    {
        private readonly IAuthorRepository _repo;
        public AuthorService(IAuthorRepository repo) => _repo = repo;

        public async Task<IEnumerable<AuthorDto>> GetAllAsync()
            => (await _repo.GetAllAsync(includeArticles: false)).Select(a => ToDto(a));

        public async Task<List<AuthorDto>> GetAll()
            => (await _repo.GetAllAsync(includeArticles: true))
               .Select(a => ToDto(a, includeArticles: true)).ToList();

        public async Task<AuthorDto?> GetByIdAsync(int id)
        {
            var a = await _repo.FindByIdAsync(id);
            return a == null ? null : ToDto(a);
        }

        public async Task<AuthorDto?> GetAuthorByUserId(string userId)
        {
            var a = await _repo.FindByUserIdAsync(userId);
            return a == null ? null : ToDto(a);
        }

        public async Task<Author?> GetBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;
            return await _repo.FindBySlugAsync(slug);
        }

        public async Task<AuthorDto> CreateAsync(AuthorDto dto)
        {
            var entity = new Author
            {
                Name = dto.Name,
                Bio = dto.Bio,
                ProfileImageUrl = dto.ProfileImageUrl,
                UserId = dto.UserId,
                Slug = NormalizeName(dto.Name),
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            await _repo.AddAsync(entity);
            dto.Id = entity.Id;
            return dto;
        }

        public async Task<AuthorDto> UpdateAsync(AuthorDto dto)
        {
            var entity = await _repo.FindByIdAsync(dto.Id)
                ?? throw new KeyNotFoundException("Author not found");
            entity.Name = dto.Name;
            entity.Bio = dto.Bio;
            entity.ProfileImageUrl = dto.ProfileImageUrl;
            entity.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(entity);
            return dto;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _repo.FindByIdAsync(id)
                ?? throw new KeyNotFoundException("Author not found");
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(entity);
        }

        public async Task UpdateImageAsync(int id, string imagePath)
        {
            var entity = await _repo.FindByIdAsync(id);
            if (entity == null) return;
            entity.ProfileImageUrl = imagePath;
            await _repo.UpdateAsync(entity);
        }

        private static AuthorDto ToDto(Author a, bool includeArticles = false) => new()
        {
            Id = a.Id,
            UserId = a.UserId,
            Name = a.Name,
            Slug = a.Slug,
            Bio = a.Bio,
            ProfileImageUrl = a.ProfileImageUrl,
            Articles = includeArticles ? a.Articles : null,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,
            IsDeleted = a.IsDeleted
        };

        private static string NormalizeName(string name)
            => name.Replace("-", " ").Trim().ToLower();

        public async Task<AuthorDto?> GetByUserIdAsync(string userId)
        {
            var a = await _repo.GetByUserIdAsync(userId);
            return a ;
        }
    }
}