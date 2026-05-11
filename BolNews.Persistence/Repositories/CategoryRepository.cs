using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Persistence.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;
        public CategoryRepository(AppDbContext context) => _context = context;

        public async Task<List<Category>> GetAllAsync(bool includeDeleted = false)
        {
            var query = _context.Categories.Include(c => c.ParentCategory).AsQueryable();
            if (!includeDeleted) query = query.Where(c => !c.IsDeleted);
            return await query.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<List<Category>> GetParentsAsync()
            => await _context.Categories
                .Where(c => c.ParentCategoryId == null && !c.IsDeleted)
                .OrderBy(c => c.Name)
                .ToListAsync();

        public async Task<List<Category>> GetParentsWithChildrenAsync()
            => await _context.Categories
                .Where(c => c.ParentCategoryId == null && !c.IsDeleted)
                .Include(c => c.SubCategories)
                .OrderBy(c => c.Name)
                .ToListAsync();

        public async Task<Category?> FindByIdAsync(int id)
            => await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        public async Task<Category?> FindBySlugAsync(string slug)
            => await _context.Categories
                .Include(c => c.Articles)
                .FirstOrDefaultAsync(c => c.Slug == slug);

        public async Task<string?> GetSlugByIdAsync(int categoryId)
            => await _context.Categories
                .Where(c => c.Id == categoryId)
                .Select(c => c.Slug)
                .FirstOrDefaultAsync();

        public async Task AddAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }
    }
}