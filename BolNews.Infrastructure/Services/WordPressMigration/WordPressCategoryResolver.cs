using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services.WordPressMigration
{
    public class WordPressCategoryResolver : IWordPressCategoryResolver
    {
        private readonly ICategoryRepository _categoryRepository;

        public WordPressCategoryResolver(
            ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Category?> ResolveAsync(
            int? wordpressCategoryId,
            string? categoryName,
            string? categorySlug,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return null;

            var name = categoryName.Trim();

            var slug = string.IsNullOrWhiteSpace(categorySlug)
                ? CreateSlug(name)
                : categorySlug.Trim().ToLowerInvariant();

            // First: find existing CMS category by slug.
            var existingCategory =
                await _categoryRepository.FindBySlugAsync(slug);

            if (existingCategory != null)
                return existingCategory;

            // Category does not exist → create it.
            var category = new Category
            {
                Name = name,
                Slug = slug,
                ParentCategoryId = null,
                MetaTitle = null,
                MetaDescription = null,
                Description = null,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _categoryRepository.AddAsync(category);

            return category;
        }

        private static string CreateSlug(string value)
        {
            return value
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "-");
        }
    }
}
