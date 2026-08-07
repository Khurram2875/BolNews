using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace BolNews.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;
        public CategoryService(ICategoryRepository repo) => _repo = repo;

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var cats = await _repo.GetAllAsync();
            return cats.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = c.ParentCategory?.Name,
                MetaTitle = c.MetaTitle,
                MetaDescription = c.MetaDescription,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsDeleted = c.IsDeleted
            });
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var c = await _repo.FindByIdAsync(id);
            if (c == null) return null;
            return new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ParentCategoryId = c.ParentCategoryId,
                MetaTitle = c.MetaTitle,
                MetaDescription = c.MetaDescription,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsDeleted = c.IsDeleted
            };
        }

        public async Task<CategoryDto> CreateAsync(CategoryDto dto)
        {
            var entity = new Category
            {
                Name = dto.Name,
                Slug = dto.Slug,
                ParentCategoryId = dto.ParentCategoryId,
                MetaTitle = dto.MetaTitle,
                MetaDescription = dto.MetaDescription,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            await _repo.AddAsync(entity);
            dto.Id = entity.Id;
            return dto;
        }

        public async Task<CategoryDto> UpdateAsync(CategoryDto dto)
        {
            var entity = await _repo.FindByIdAsync(dto.Id)
                ?? throw new KeyNotFoundException("Category not found");
            entity.Name = dto.Name;
            entity.Slug = dto.Slug;
            entity.ParentCategoryId = dto.ParentCategoryId;
            entity.MetaTitle = dto.MetaTitle;
            entity.MetaDescription = dto.MetaDescription;
            entity.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(entity);
            return dto;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _repo.FindByIdAsync(id)
                ?? throw new KeyNotFoundException("Category not found");
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(entity);
        }

        public async Task<string> GetCategorySlug(int id) => await _repo.GetSlugByIdAsync(id) ?? string.Empty;
        public async Task<Category> GetBySlugAsync(string slug) => await _repo.FindBySlugAsync(slug);
        public async Task<List<Category>> GetAllAsyncNew() => await _repo.GetAllAsync(includeDeleted: true);
        public async Task<List<Category>> GetHomeCategoriesAsync() => await _repo.GetParentsAsync();
        public async Task<List<Category>> GetParentCategoriesAsync() => await _repo.GetParentsAsync();
        public async Task<List<Category>> GetParentCategoriesWithChildrenAsync() => await _repo.GetParentsWithChildrenAsync();
    }
}