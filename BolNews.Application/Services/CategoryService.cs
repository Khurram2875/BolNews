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
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            return await _context.Categories
                .Where(c => !c.IsDeleted)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ParentCategoryId = c.ParentCategoryId,
                    ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    IsDeleted = c.IsDeleted
                })
                .ToListAsync();
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            return await _context.Categories
                .Where(c => c.Id == id && !c.IsDeleted)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ParentCategoryId = c.ParentCategoryId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    IsDeleted = c.IsDeleted
                })
                .FirstOrDefaultAsync();
        }

        public async Task<CategoryDto> CreateAsync(CategoryDto dto)
        {
            var entity = new Category
            {
                Name = dto.Name,
                Slug = dto.Slug,
                ParentCategoryId = dto.ParentCategoryId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Categories.Add(entity);
            await _context.SaveChangesAsync();

            dto.Id = entity.Id;
            return dto;
        }

        public async Task<CategoryDto> UpdateAsync(CategoryDto dto)
        {
            var entity = await _context.Categories.FindAsync(dto.Id);
            if (entity == null || entity.IsDeleted)
                throw new KeyNotFoundException("Category not found");

            entity.Name = dto.Name;
            entity.Slug = dto.Slug;
            entity.ParentCategoryId = dto.ParentCategoryId;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return dto;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Categories.FindAsync(id);
            if (entity == null || entity.IsDeleted)
                throw new KeyNotFoundException("Category not found");

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<string> GetCategorySlug(int catId)
        {
            // Fix: Use FirstOrDefaultAsync to get a single string result asynchronously
            var catSlug = await _context.Categories
                .Where(x => x.Id == catId)
                .Select(x => x.Slug)
                .FirstOrDefaultAsync();

            return catSlug;
        }
        public async Task<Category> GetBySlugAsync(string slug)
        {
            return await _context.Categories
                .Include(a=>a.Articles)
                .FirstOrDefaultAsync(c => c.Slug == slug);
        }
        public async Task<List<Category>> GetAllAsyncNew()
        {
            return await _context.Categories.ToListAsync();
        }
        public async Task<List<Category>> GetHomeCategoriesAsync()
        {
            return await _context.Categories
                .Where(c => c.ParentCategoryId == null) // only main categories
                .ToListAsync();
        }
        public async Task<List<Category>> GetParentCategoriesAsync()
        {
            return await _context.Categories
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
        public async Task<List<Category>> GetParentCategoriesWithChildrenAsync()
        {
            return await _context.Categories
                .Where(c => c.ParentCategoryId == null)
                .Include(c => c.SubCategories) // IMPORTANT
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}
