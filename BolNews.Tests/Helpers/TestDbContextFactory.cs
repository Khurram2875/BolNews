using BolNews.Domain.Entities;
using BolNews.Domain.Entities.Base;
using BolNews.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Tests.Helpers
{
    /// <summary>
    /// Provides a fresh in-memory AppDbContext for each test.
    /// Each call returns a unique database name so tests are fully isolated.
    /// </summary>
    public static class TestDbContextFactory
    {
        public static AppDbContext Create(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        /// <summary>Seeds a minimal set of entities needed by most tests.</summary>
        public static AppDbContext CreateWithSeed()
        {
            var context = Create();

            var user1 = new ApplicationUser
            {
                Id       = "user-author-1",
                UserName = "author1@test.com",
                Email    = "author1@test.com",
                FullName = "Test Author One"
            };
            var user2 = new ApplicationUser
            {
                Id       = "user-author-2",
                UserName = "author2@test.com",
                Email    = "author2@test.com",
                FullName = "Test Author Two"
            };
            context.Users.AddRange(user1, user2);

            var category = new Category
            {
                Id        = 1,
                Name      = "Technology",
                Slug      = "technology",
                CreatedAt = DateTime.UtcNow
            };
            context.Categories.Add(category);

            var author1 = new Author
            {
                Id        = 1,
                UserId    = "user-author-1",
                User      = user1,
                Name      = "Test Author One",
                Slug      = "test-author-one",
                Bio       = "Bio for author one",
                CreatedAt = DateTime.UtcNow
            };
            var author2 = new Author
            {
                Id        = 2,
                UserId    = "user-author-2",
                User      = user2,
                Name      = "Test Author Two",
                Slug      = "test-author-two",
                Bio       = "Bio for author two",
                CreatedAt = DateTime.UtcNow
            };
            context.Authors.AddRange(author1, author2);

            var articles = new[]
            {
                new Article
                {
                    Id          = 1,
                    Title       = "First Technology Article",
                    Slug        = "first-technology-article",
                    MetaTitle   = "First Technology Article",
                    MetaDescription = "First Techonology Description",
                    Summary     = "Summary one",
                    Content     = "Content one",
                    CategoryId  = 1,
                    AuthorId    = 1,
                    IsPublished = true,
                    PublishedAt = DateTime.UtcNow.AddDays(-1),
                    ViewCount   = 100,
                    CreatedAt   = DateTime.UtcNow.AddDays(-1)
                },
                new Article
                {
                    Id          = 2,
                    Title       = "Second Technology Article",
                    Slug        = "second-technology-article",
                    MetaTitle   = "Second Technology Article",
                    MetaDescription = "Second Techonology Description",
                    Summary     = "Summary two",
                    Content     = "Content two",
                    CategoryId  = 1,
                    AuthorId    = 1,
                    IsPublished = true,
                    PublishedAt = DateTime.UtcNow.AddDays(-2),
                    ViewCount   = 50,
                    CreatedAt   = DateTime.UtcNow.AddDays(-2)
                },
                new Article
                {
                    Id          = 3,
                    Title       = "Draft Article — Not Published",
                    Slug        = "draft-article",
                    MetaTitle   = "Draft Article — Not Published",
                    MetaDescription = "Draft Article — Not Published Description",
                    Summary     = "Draft summary",
                    Content     = "Draft content",
                    CategoryId  = 1,
                    AuthorId    = 2,
                    IsPublished = false,
                    ViewCount   = 0,
                    CreatedAt   = DateTime.UtcNow
                },
                new Article
                {
                    Id          = 4,
                    Title       = "Deleted Article",
                    Slug        = "deleted-article",
                    MetaTitle   = "Deleted Article",
                    MetaDescription = "Deleted Article Description",
                    Summary     = "Deleted summary",
                    Content     = "Deleted content",
                    CategoryId  = 1,
                    AuthorId    = 1,
                    IsPublished = true,
                    IsDeleted   = true,
                    PublishedAt = DateTime.UtcNow.AddDays(-3),
                    ViewCount   = 200,
                    CreatedAt   = DateTime.UtcNow.AddDays(-3)
                }
            };
            context.Articles.AddRange(articles);
            context.SaveChanges();

            return context;
        }
    }
}
