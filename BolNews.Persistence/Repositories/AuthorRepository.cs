using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Persistence.Repositories
{
    public class AuthorRepository : IAuthorRepository
    {
        private readonly AppDbContext _context;
        public AuthorRepository(AppDbContext context) => _context = context;

        public async Task<List<Author>> GetAllAsync(bool includeArticles = false)
        {
            var query = _context.Authors.Where(a => !a.IsDeleted).AsQueryable();
            if (includeArticles) query = query.Include(a => a.Articles);
            return await query.ToListAsync();
        }

        public async Task<Author?> FindByIdAsync(int id)
            => await _context.Authors.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        public async Task<Author?> FindByUserIdAsync(string userId)
            => await _context.Authors.FirstOrDefaultAsync(a => a.UserId == userId && !a.IsDeleted);

        public async Task<Author?> FindBySlugAsync(string slug)
            => await _context.Authors
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Slug == slug);

        public async Task AddAsync(Author author)
        {
            _context.Authors.Add(author);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Author author)
        {
            _context.Authors.Update(author);
            await _context.SaveChangesAsync();
        }
    }
}
