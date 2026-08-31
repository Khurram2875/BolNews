using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Persistence.Repositories
{
    public class ReporterRepository : IReporterRepository
    {
        private readonly AppDbContext _context;

        public ReporterRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Reporter>> GetAllAsync()
        {
            return await _context.Reporters
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<Reporter?> FindByIdAsync(int id)
        {
            return await _context.Reporters
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public async Task AddAsync(Reporter reporter)
        {
            await _context.Reporters.AddAsync(reporter);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Reporter reporter)
        {
            _context.Reporters.Update(reporter);
            await _context.SaveChangesAsync();
        }
        public async Task<Reporter?> FindBySlugAsync(string slug)
        {
            return await _context.Reporters
                .FirstOrDefaultAsync(x =>
                    x.Slug == slug &&
                    !x.IsDeleted);
        }
    }
}
