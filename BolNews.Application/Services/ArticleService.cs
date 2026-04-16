using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Common.Helpers;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
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
                FeaturedImageUrl = dto.FeaturedImageUrl,
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
            article.FeaturedImageUrl = dto.FeaturedImageUrl;
            article.CategoryId = dto.CategoryId;
            article.AuthorId = dto.AuthorId;
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
                    FeaturedImageUrl = x.FeaturedImageLarge,
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

        public async Task<IEnumerable<ArticleDto>> GetAllAsync()
        {
            return await _context.Articles
           .Include(a => a.Author)
           .Include(a => a.Category)
           .Where(a => !a.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
           .Select(a => new ArticleDto
           {
               Id = a.Id,
               Title = a.Title,
               Slug = a.Slug,
               Summary = a.Summary,
               Content = a.Content,
               FeaturedImageUrl = a.FeaturedImageUrl,
               FeaturedImageThumb= a.FeaturedImageThumb,
               FeaturedImageMedium = a.FeaturedImageMedium,
               FeaturedImageLarge = a.FeaturedImageLarge,
               AuthorId = a.AuthorId,
               CategoryId = a.CategoryId,

               IsPublished = a.IsPublished,
               PublishedAt = a.PublishedAt,

               // 🔥 IMPORTANT PART
               AuthorName = a.Author.Name != null ? a.Author.Name : null,
               CategoryName = a.Category.Name != null ? a.Category.Name : null
           })
          
           .ToListAsync();
        }
        public async Task UpdateImagesAsync(int id, string thumb, string medium, string large)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article != null)
            {
                article.FeaturedImageThumb = thumb;
                article.FeaturedImageMedium = medium;
                article.FeaturedImageLarge = large;

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
            return await _context.Articles
                .Include(c=>c.Category)
         .FirstOrDefaultAsync(a => a.Slug == slug && !a.IsDeleted);
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
    }
}
