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
    public class ArticleRevisionRepository : IArticleRevisionRepository
    {
        private readonly AppDbContext _context;

        public ArticleRevisionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ArticleRevision revision)
        {
            _context.ArticleRevisions.Add(revision);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetNextRevisionNumberAsync(int articleId)
        {
            var maxRevision = await _context.ArticleRevisions
                .Where(x => x.ArticleId == articleId)
                .Select(x => (int?)x.RevisionNumber)
                .MaxAsync();

            return (maxRevision ?? 0) + 1;
        }

        public async Task<List<ArticleRevision>> GetByArticleIdAsync(int articleId)
        {
            return await _context.ArticleRevisions
                .Where(x => x.ArticleId == articleId)
                .OrderByDescending(x => x.RevisionNumber)
                .ToListAsync();
        }
    }
}
