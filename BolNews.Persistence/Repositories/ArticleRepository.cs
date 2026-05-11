using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Persistence.Repositories
{
    public class ArticleRepository : IArticleRepository
    {
        private readonly AppDbContext _context;
        public ArticleRepository(AppDbContext context) => _context = context;

        public async Task<int> AddAsync(Article article)
        {
            _context.Articles.Add(article);
            await _context.SaveChangesAsync();
            return article.Id;
        }

        public async Task UpdateAsync(Article article)
        {
            _context.Articles.Update(article);
            await _context.SaveChangesAsync();
        }

        public async Task<Article?> FindByIdAsync(int id)
            => await _context.Articles.FindAsync(id);

        public async Task<bool> SlugExistsAsync(string slug)
            => await _context.Articles.AnyAsync(a => a.Slug == slug);

        public async Task<Article?> FindBySlugAsync(string slug)
            => await _context.Articles
                .AsNoTrackingWithIdentityResolution()
                .Include(a => a.Category)
                .Include(a => a.Author)
                .FirstOrDefaultAsync(a => a.Slug == slug && !a.IsDeleted);

        public async Task<List<Article>> GetAllAsync()
            => await _context.Articles
                .Include(a => a.Author).ThenInclude(a => a.User)
                .Include(a => a.Category)
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

        public async Task<List<Article>> GetByAuthorIdAsync(int authorId, int page, int pageSize)
            => await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.AuthorId == authorId && a.IsPublished)
                .OrderByDescending(a => a.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        public async Task<List<Article>> GetByCategorySlugAsync(string categorySlug, int page, int pageSize)
            => await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Where(a => a.Category.Slug == categorySlug && !a.IsDeleted)
                .OrderByDescending(a => a.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        public async Task<List<Article>> GetPublishedAsync(int count)
            => await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.IsPublished && !a.IsDeleted)
                .OrderByDescending(a => a.PublishedAt)
                .Take(count)
                .ToListAsync();

        public async Task<List<Article>> GetByCategoryIdAsync(int categoryId, int count)
            => await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.CategoryId == categoryId && a.IsPublished && !a.IsDeleted)
                .OrderByDescending(a => a.PublishedAt)
                .Take(count)
                .ToListAsync();

        public async Task<List<Article>> GetForCategoriesAsync(List<int> categoryIds)
            => await _context.Articles
                .Include(a => a.Category)
                .Where(a => categoryIds.Contains(a.CategoryId) && a.IsPublished && !a.IsDeleted)
                .OrderByDescending(a => a.PublishedAt)
                .ToListAsync();

        public async Task<List<Article>> GetPublishedSinceAsync(DateTime fromDate, int limit)
            => await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.PublishedAt >= fromDate && a.IsPublished)
                .OrderByDescending(a => a.PublishedAt)
                .Take(limit)
                .ToListAsync();

        public async Task<List<Article>> SearchAsync(string term, int page, int pageSize)
            => await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.IsPublished && !a.IsDeleted &&
                    (EF.Functions.Like(a.Title, $"%{term}%") ||
                     EF.Functions.Like(a.Content, $"%{term}%")))
                .OrderByDescending(a => a.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        public async Task<List<Article>> GetTrendingCandidatesAsync(DateTime fromDate, int candidateLimit)
            => await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.IsPublished && !a.IsDeleted && a.PublishedAt >= fromDate)
                .OrderByDescending(a => a.ViewCount)
                .Take(candidateLimit)
                .ToListAsync();

        public async Task<List<Article>> GetTopByViewCountAsync(int count)
            => await _context.Articles
                .Where(a => !a.IsDeleted && a.IsPublished)
                .OrderByDescending(a => a.ViewCount)
                .Take(count)
                .ToListAsync();

        public async Task<List<Article>> GetLowPerformingAsync(DateTime since, int maxViews, int limit)
            => await _context.Articles
                .Where(a => a.PublishedAt >= since && a.ViewCount < maxViews)
                .OrderByDescending(a => a.PublishedAt)
                .Take(limit)
                .ToListAsync();

        public async Task<int> CountAsync()
            => await _context.Articles.CountAsync(a => !a.IsDeleted);

        public async Task<int> CountPublishedSinceAsync(DateTime since)
            => await _context.Articles.CountAsync(a => a.PublishedAt >= since && !a.IsDeleted);

        public async Task<List<(DateTime Date, int Count)>> CountPerDayAsync(DateTime fromDate)
        {
            var data = await _context.Articles
                .Where(a => a.PublishedAt >= fromDate && !a.IsDeleted)
                .GroupBy(a => a.PublishedAt!.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();
            return data.Select(x => (x.Date, x.Count)).ToList();
        }

        public async Task<List<CategoryPerformanceDto>> GetCategoryPerformanceAsync(DateTime fromDate)
            => await _context.Articles
                .Where(a => a.PublishedAt >= fromDate && !a.IsDeleted && a.IsPublished)
                .Include(a => a.Category)
                .GroupBy(a => a.Category.Name)
                .Select(g => new CategoryPerformanceDto
                {
                    CategoryName = g.Key,
                    ArticleCount = g.Count(),
                    TotalViews = g.Sum(a => a.ViewCount),
                    AvgViewsPerArticle = g.Average(a => a.ViewCount)
                })
                .OrderByDescending(x => x.TotalViews)
                .ToListAsync();

        public async Task<List<EditorPerformanceDto>> GetEditorPerformanceAsync(DateTime fromDate)
            => await _context.Articles
                .Where(a => a.PublishedAt >= fromDate && !a.IsDeleted && a.IsPublished)
                .Include(a => a.Author)
                .GroupBy(a => a.Author.Name)
                .Select(g => new EditorPerformanceDto
                {
                    AuthorName = g.Key,
                    ArticleCount = g.Count(),
                    TotalViews = g.Sum(a => a.ViewCount),
                    AvgViewsPerArticle = g.Average(a => a.ViewCount)
                })
                .OrderByDescending(x => x.TotalViews)
                .ToListAsync();

        public async Task IncrementViewCountAsync(int articleId)
            => await _context.Articles
                .Where(a => a.Id == articleId)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(a => a.ViewCount, a => a.ViewCount + 1));
    }
}