using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Persistence.Repositories
{
    public class BreakingNewsRepository : IBreakingNewsRepository
    {
        private readonly AppDbContext _context;

        public BreakingNewsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<BreakingNews>> GetAllAsync()
        {
            var now = DateTime.UtcNow;
            var result = await _context.BreakingNews
        .Include(x => x.Article)
            .ThenInclude(a => a.Category)
        .OrderByDescending(x => x.IsPinned)
        .ThenBy(x => x.DisplayOrder)
        .ThenByDescending(x => x.CreatedAt)
        .ToListAsync();
            return result;
        }

        public async Task<List<BreakingNews>> GetActiveAsync()
        {
            var now = DateTime.UtcNow.AddHours(5);

            //    return await _context.BreakingNews
            //.Include(x => x.Article)
            //    .ThenInclude(a => a.Category)
            //.Where(x => x.IsActive)
            //.ToListAsync();
            var result = await _context.BreakingNews
                 .Include(x => x.Article)
                     .ThenInclude(a => a.Category)
                 .Where(x =>
                     x.IsActive &&
                     (x.StartDate == null || x.StartDate <= now) &&
                     (x.EndDate == null || x.EndDate >= now))
                 .OrderByDescending(x => x.IsPinned)
                 .ThenBy(x => x.DisplayOrder)
                 .ThenByDescending(x => x.CreatedAt)
                 .ToListAsync();
            return result;
        }

        public async Task<List<BreakingNews>> GetInactiveAsync()
        {
            return await _context.BreakingNews
                .Include(x => x.Article)
                .Where(x => !x.IsActive)
                .OrderByDescending(x => x.IsPinned)
                .ThenBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<BreakingNews?> GetByIdAsync(int id)
        {
            // TODO: Replace with centralized DateTimeProvider during CMS time standardization.
            var now = DateTime.UtcNow;
            return await _context.BreakingNews
                .Include(x => x.Article)
                .Include(x => x.CreatedByUser)
                .Include(x => x.UpdatedByUser)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(BreakingNews entity)
        {
            await _context.BreakingNews.AddAsync(entity);
        }

        public Task UpdateAsync(BreakingNews entity)
        {
            _context.BreakingNews.Update(entity);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}