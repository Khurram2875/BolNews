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
    public class ArticleService : IArticleService
    {
        private readonly AppDbContext _context;

        public ArticleService(AppDbContext context)
        {
            _context = context;
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
    }
}
