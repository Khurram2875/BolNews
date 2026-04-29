using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Common.Helpers;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;


namespace BolNews.Application.Services
{
    public class ArticleService : IArticleService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;

        public ArticleService(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }
        public async Task<int> CreateAsync(ArticleDto dto)
        {
            var article = new Article
            {
                Title = dto.Title,
                Slug = dto.Slug,
                Summary = dto.Summary,
                Content = dto.Content,
                FeaturedImageLarge = dto.FeaturedImageLarge,
                MetaDescription = dto.MetaDescription,
                MetaTitle = dto.MetaTitle,
                CategoryId = dto.CategoryId,
                AuthorId = dto.AuthorId,
                IsPublished = dto.IsPublished,
                PublishedAt = dto.IsPublished ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            return article.Id;
        }

        public async Task UpdateAsync(ArticleDto dto)
        {
            var article = await _context.Articles.FindAsync(dto.Id);

            if (article == null) return;

            article.Title = dto.Title;
            article.Slug = dto.Slug;
            article.Summary = dto.Summary;
            article.Content = dto.Content;
            article.MetaTitle = dto.MetaTitle;
            article.MetaDescription = dto.MetaDescription;
            article.FeaturedImageXl = dto.FeaturedImageXl;
            article.CategoryId = dto.CategoryId;
            //article.AuthorId = dto.AuthorId;
            article.IsPublished = dto.IsPublished;

            if (dto.IsPublished && article.PublishedAt == null)
                article.PublishedAt = DateTime.UtcNow;

            article.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);

            if (article == null) return;

            article.IsDeleted = true;
            await _context.SaveChangesAsync();
        }

        public async Task<ArticleDto?> GetByIdAsync(int id)
        {
            return await _context.Articles
                .Where(x => x.Id == id)
                .Select(x => new ArticleDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Slug = x.Slug,
                    Summary = x.Summary,
                    Content = x.Content,
                    MetaTitle = x.MetaTitle,
                    MetaDescription=x.MetaDescription,
                    FeaturedImageXl = x.FeaturedImageXl,
                    FeaturedImageLarge= x.FeaturedImageLarge,
                    FeaturedImageMedium = x.FeaturedImageMedium,
                    FeaturedImageThumb = x.FeaturedImageThumb,
                    CategoryId = x.CategoryId,
                    AuthorId = x.AuthorId,
                    IsPublished = x.IsPublished,
                    PublishedAt = x.PublishedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<ArticleDto>> GetAllAsync(string userId, IList<string> roles)
        {
            var query = _context.Articles
        .Include(a => a.Author)
        .Include(a => a.Category)
        .Where(a => !a.IsDeleted)
        .AsQueryable();

            // 🔥 Admin & Editor → see ALL
            if (roles.Contains("Admin") || roles.Contains("Editor"))
            {
                query = query;
            }
            // 🔥 Author → only own articles
            else if (roles.Contains("Author"))
            {
                var author = await _context.Authors
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (author == null)
                    return new List<ArticleDto>();

                query = query.Where(a => a.AuthorId == author.Id);
            }
            // 🔥 SubEditor → define behavior (for now: all)
            else if (roles.Contains("SubEditor"))
            {
                // optionally filter later
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(a => new ArticleDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Slug = a.Slug,
                    Summary = a.Summary,
                    Content = a.Content,
                    FeaturedImageThumb = a.FeaturedImageThumb,
                    FeaturedImageMedium = a.FeaturedImageMedium,
                    FeaturedImageLarge = a.FeaturedImageLarge,
                    AuthorId = a.AuthorId,
                    CategoryId = a.CategoryId,

                    IsPublished = a.IsPublished,
                    PublishedAt = a.PublishedAt,

                    // 👇 safer null handling
                    AuthorName = a.Author != null ? a.Author.User.FullName : null,
                    CategoryName = a.Category != null ? a.Category.Name : null
                })
                .ToListAsync();
            // return await _context.Articles
            //.Include(a => a.Author)
            //.Include(a => a.Category)
            //.Where(a => !a.IsDeleted)
            // .OrderByDescending(x => x.CreatedAt)
            //.Select(a => new ArticleDto
            //{
            //    Id = a.Id,
            //    Title = a.Title,
            //    Slug = a.Slug,
            //    Summary = a.Summary,
            //    Content = a.Content,
            //    FeaturedImageThumb = a.FeaturedImageThumb,
            //    FeaturedImageMedium = a.FeaturedImageMedium,
            //    FeaturedImageLarge = a.FeaturedImageLarge,
            //    AuthorId = a.AuthorId,
            //    CategoryId = a.CategoryId,

            //    IsPublished = a.IsPublished,
            //    PublishedAt = a.PublishedAt,

            //     //🔥 IMPORTANT PART
            //    AuthorName = a.Author.Name != null ? a.Author.Name : null,
            //    CategoryName = a.Category.Name != null ? a.Category.Name : null
            //})

            //.ToListAsync();
        }
        public async Task UpdateImagesAsync(int id, string thumb, string medium, string large, string xl)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article != null)
            {
                article.FeaturedImageThumb = thumb;
                article.FeaturedImageMedium = medium;
                article.FeaturedImageLarge = large;
                article.FeaturedImageXl = xl;

                await _context.SaveChangesAsync();
            }
        }
        public async Task<string> GenerateUniqueSlugAsync(string title)
        {
            var baseSlug = SlugHelper.GenerateSlug(title);
            var slug = baseSlug;
            int count = 1;

            while (await _context.Articles.AnyAsync(a => a.Slug == slug))
            {
                slug = $"{baseSlug}-{count}";
                count++;
            }

            return slug;
        }
        public async Task<Article> GetBySlugAsync(string slug)
        {
            var query = _context.Articles
                       .Include(a => a.Category)
                       .Where(a => a.Slug == slug && !a.IsDeleted)
                       .AsQueryable();
            var article = await _context.Articles
                  .Include(c => c.Category)
                  .Include(a=>a.Author)
                  .FirstOrDefaultAsync(a => a.Slug == slug && !a.IsDeleted);
            return article;
        }
        public async Task<List<Article>> GetByCategorySlugAsync(string categorySlug, int page)
        {
            int pageSize = 10;

            var article = await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Where(a => a.Category.Slug == categorySlug && !a.IsDeleted)
                .OrderByDescending(a => a.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return article;
        }

        public async Task<List<Article>> GetRelatedArticlesAsync(int categoryId, int excludeArticleId, int count = 5)
        {
            string cacheKey = $"related_{categoryId}_{excludeArticleId}";

            if (_cache.TryGetValue(cacheKey, out List<Article> cached))
                return cached;

            var articles = await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.CategoryId == categoryId
                            && a.Id != excludeArticleId
                            && a.IsPublished
                            && !a.IsDeleted)
                .OrderByDescending(a => a.PublishedAt)
                .Take(count)
                .ToListAsync();

            _cache.Set(cacheKey, articles, TimeSpan.FromMinutes(5));

            return articles;
        }
        public async Task<List<Article>> GetLatestArticlesAsync(int count = 8)
        {
            return await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.IsPublished && !a.IsDeleted)
                .OrderByDescending(a => a.PublishedAt)
                .Take(count)
                .ToListAsync();
        }
        public async Task<List<Article>> GetAllPublishedAsync()
        {
            return await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.IsPublished && !a.IsDeleted)
                .ToListAsync();
        }
        public async Task<Article?> GetTopStoryAsync()
        {
            return await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.IsPublished && !a.IsDeleted)
                .OrderByDescending(a => a.PublishedAt)
                .FirstOrDefaultAsync();
        }
        public async Task<List<Article>> GetSecondaryStoriesAsync(int count = 4)
        {
            return await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.IsPublished && !a.IsDeleted)
                .OrderByDescending(a => a.PublishedAt)
                .Skip(1)
                .Take(count)
                .ToListAsync();
        }
        

        public async Task<List<Article>> GetArticlesByCategoryAsync(int categoryId, int count = 5)
        {
            return await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.CategoryId == categoryId
                            && a.IsPublished
                            && !a.IsDeleted)
                .OrderByDescending(a => a.PublishedAt)
                .Take(count)
                .ToListAsync();
        }
        public async Task<Dictionary<int, List<Article>>> GetArticlesForCategoriesAsync(List<int> categoryIds, int count)
        {
            var articles = await _context.Articles
                .Include(a => a.Category)
                .Where(a => categoryIds.Contains(a.CategoryId)
                            && a.IsPublished
                            && !a.IsDeleted)
                .OrderByDescending(a => a.PublishedAt)
                .ToListAsync();

            return articles
                .GroupBy(a => a.CategoryId)
                .ToDictionary(g => g.Key, g => g.Take(count).ToList());
        }
        public async Task<List<Article>> GetBreakingNewsAsync(int count = 5)
        {
            return await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.IsPublished && !a.IsDeleted)
                .OrderByDescending(a => a.PublishedAt)
                .Take(count)
                .ToListAsync();
        }
        public async Task<List<Article>> SearchAsync(string query, int page, int pageSize)
        {
            var normalized = query.Trim().ToLower();

            return await _context.Articles
                .Include(a => a.Category)
                .Where(a =>
                    a.IsPublished &&
                    !a.IsDeleted &&
                    (
                        a.Title.ToLower().Contains(normalized) ||
                        a.Content.ToLower().Contains(normalized)
                    ))
                .OrderByDescending(a => a.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        public async Task IncrementViewCountAsync(int articleId)
        {
            var article = await _context.Articles.FindAsync(articleId);

            if (article != null)
            {
                article.ViewCount++;
                await _context.SaveChangesAsync();
            }
        }
        //public async Task<List<Article>> GetTrendingAsync(int count = 5)
        //{
        //    var last7Days = DateTime.UtcNow.AddDays(-7);

        //    return await _context.Articles
        //        .Include(a => a.Category)
        //        .Where(a => a.IsPublished &&
        //                    !a.IsDeleted &&
        //                    a.PublishedAt >= last7Days)
        //        .OrderByDescending(a => a.ViewCount)
        //        .Take(count)
        //        .ToListAsync();
        //}
        public async Task<List<Article>> GetTrendingAsync(int count = 5, string type = "week")
        {
            DateTime fromDate = type switch
            {
                "today" => DateTime.UtcNow.AddDays(-1),
                "week" => DateTime.UtcNow.AddDays(-7),
                "month" => DateTime.UtcNow.AddDays(-30),
                _ => DateTime.UtcNow.AddDays(-7)
            };

            var articles = await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.IsPublished &&
                            !a.IsDeleted &&
                            a.PublishedAt >= fromDate)
                .ToListAsync();

            var now = DateTime.UtcNow;

            return articles
                .Select(a =>
                {
                    var hours = (now - a.PublishedAt.Value).TotalHours;
                    var recencyScore = 1 / (1 + hours);

                    var score = a.ViewCount + (recencyScore * 200);

                    return new { a, score };
                })
                .OrderByDescending(x => x.score)
                .Take(count)
                .Select(x => x.a)
                .ToList();
        }
        public async Task<List<Article>> GetLatestPublishedAsync(DateTime fromDate, int limit)
        {
            return await _context.Articles
                .Where(a => a.PublishedAt >= fromDate && a.IsPublished == true)
                .OrderByDescending(a => a.PublishedAt)
                .Include(a => a.Category)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Article>> GetRecentArticlesAsync(int hours = 48)
        {
            var fromDate = DateTime.UtcNow.AddHours(-hours);

            return await _context.Articles
                .Where(a => a.PublishedAt >= fromDate && a.IsPublished == true)
                .Include(a => a.Category)
                .OrderByDescending(a => a.ViewCount)
                .ThenByDescending(a => a.PublishedAt)
                .Take(200)
                .ToListAsync();
        }
        public async Task<int> GetTotalArticlesAsync()
        {
            return await _context.Articles.CountAsync(a => !a.IsDeleted);
        }

        public async Task<int> GetTodayArticlesCountAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Articles
                .CountAsync(a => a.PublishedAt >= today && !a.IsDeleted);
        }

        public async Task<List<Article>> GetTopArticlesAsync(int count = 10)
        {
            return await _context.Articles
                .Where(a => !a.IsDeleted && a.IsPublished == true)
                .OrderByDescending(a => a.ViewCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Article>> GetLowPerformingArticlesAsync()
        {
            var since = DateTime.UtcNow.AddDays(-2);

            return await _context.Articles
                .Where(a => a.PublishedAt >= since && a.ViewCount < 50)
                .OrderByDescending(a => a.PublishedAt)
                .Take(10)
                .ToListAsync();
        }
        public async Task<List<(DateTime date, int count)>> GetArticlesPerDayAsync(int days = 7)
        {
            var fromDate = DateTime.UtcNow.Date.AddDays(-days);

            var data = await _context.Articles
                .Where(a => a.PublishedAt >= fromDate && !a.IsDeleted)
                .GroupBy(a => a.PublishedAt.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return data.Select(x => (x.Date, x.Count)).ToList();
        }
        public async Task<List<CategoryPerformanceDto>> GetCategoryPerformanceAsync(int days = 7)
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);

            return await _context.Articles
                .Where(a => a.PublishedAt >= fromDate && !a.IsDeleted && a.IsPublished == true)
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
        }
        public async Task<List<EditorPerformanceDto>> GetEditorPerformanceAsync(int days = 7)
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);

            return await _context.Articles
                .Where(a => a.PublishedAt >= fromDate &&
                            !a.IsDeleted &&
                            a.IsPublished == true)
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
        }
        public async Task<bool> CanEditAsync(int articleId, string userId, IList<string> roles)
        {
            var article = await _context.Articles
                .Include(a => a.Author)
                .FirstOrDefaultAsync(a => a.Id == articleId);

            if (article == null)
                return false;

            // Admin & Editor → full access
            if (roles.Contains("Admin") || roles.Contains("Editor"))
                return true;

            // SubEditor → can edit but not publish
            if (roles.Contains("SubEditor"))
                return true;

            // Author → only own article
            if (roles.Contains("Author"))
            {
                return article.Author.UserId == userId;
            }

            return false;
        }
        public async Task<bool> CanDeleteAsync(int articleId, string userId, IList<string> roles)
        {
            var article = await _context.Articles
                .Include(a => a.Author)
                .FirstOrDefaultAsync(a => a.Id == articleId);

            if (article == null)
                return false;

            // Admin & Editor → full access
            if (roles.Contains("Admin") || roles.Contains("Editor"))
                return true;

            // Author → only own article
            if (roles.Contains("Author"))
                return article.Author.UserId == userId;

            // SubEditor → no delete
            return false;
        }
        public async Task<List<Article>> GetByAuthorAsync(int authorId)
        {
            return await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.AuthorId == authorId && a.IsPublished)
                .OrderByDescending(a => a.PublishedAt)
                .Take(20)
                .ToListAsync();
        }   
    }
}
