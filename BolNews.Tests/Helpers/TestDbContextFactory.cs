using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;




namespace BolNews.Tests.Helpers
{
    /// <summary>
    /// Creates AppDbContext instances for tests.
    ///
    /// TWO PROVIDERS:
    ///   CreateWithSeed()     — EF Core InMemory. Fast. Use for most tests.
    ///   CreateSqlite()       — SQLite in-memory. Use when the test calls
    ///                          ExecuteUpdateAsync / ExecuteDeleteAsync,
    ///                          which the InMemory provider does not support.
    /// </summary>
    public static class TestDbContextFactory
    {
        // ── InMemory (default) ────────────────────────────────────────────────

        public static AppDbContext Create(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        public static AppDbContext CreateWithSeed()
        {
            var context = Create();
            SeedData(context);
            return context;
        }

        // ── SQLite in-memory ──────────────────────────────────────────────────

        /// <summary>
        /// Returns a SQLite in-memory context whose connection stays open for
        /// the lifetime of the returned SqliteConnection.
        /// Caller must dispose both objects: await using var (ctx, conn) = ...
        /// </summary>
        public static (AppDbContext Context, SqliteConnection Connection) CreateSqlite()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
           
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return (context, connection);
        }

        public static (AppDbContext Context, SqliteConnection Connection) CreateSqliteWithSeed()
        {
            var (context, connection) = CreateSqlite();
            SeedData(context);
            return (context, connection);
        }

        // ── Shared seed data ──────────────────────────────────────────────────

        private static void SeedData(AppDbContext context)
        {
            var user1 = new ApplicationUser
            {
                Id = "user-author-1",
                UserName = "author1@test.com",
                Email = "author1@test.com",
                FullName = "Test Author One"
            };
            var user2 = new ApplicationUser
            {
                Id = "user-author-2",
                UserName = "author2@test.com",
                Email = "author2@test.com",
                FullName = "Test Author Two"
            };
            context.Users.AddRange(user1, user2);

            var category = new Category
            {
                Id = 1,
                Name = "Technology",
                Slug = "technology",
                CreatedAt = DateTime.UtcNow
            };
            context.Categories.Add(category);

            var author1 = new Author
            {
                Id = 1,
                UserId = "user-author-1",
                User = user1,
                Name = "Test Author One",
                Slug = "test-author-one",
                Bio = "Bio for author one",
                CreatedAt = DateTime.UtcNow
            };
            var author2 = new Author
            {
                Id = 2,
                UserId = "user-author-2",
                User = user2,
                Name = "Test Author Two",
                Slug = "test-author-two",
                Bio = "Bio for author two",
                CreatedAt = DateTime.UtcNow
            };
            context.Authors.AddRange(author1, author2);

            context.Articles.AddRange(
                new Article
                {
                    Id = 1,
                    Title = "First Technology Article",
                    Slug = "first-technology-article",
                    MetaTitle = "First Technology Article",
                    MetaDescription = "First Technology Description",
                    Summary = "Summary one",
                    Content = "Content one",
                    CategoryId = 1,
                    AuthorId = 1,
                    IsPublished = true,
                    PublishedAt = DateTime.UtcNow.AddDays(-1),
                    ViewCount = 100,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Article
                {
                    Id = 2,
                    Title = "Second Technology Article",
                    Slug = "second-technology-article",
                    MetaTitle = "Second Technology Article",
                    MetaDescription = "Second Technology Description",
                    Summary = "Summary two",
                    Content = "Content two",
                    CategoryId = 1,
                    AuthorId = 1,
                    IsPublished = true,
                    PublishedAt = DateTime.UtcNow.AddDays(-2),
                    ViewCount = 50,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Article
                {
                    Id = 3,
                    Title = "Draft Article — Not Published",
                    Slug = "draft-article",
                    MetaTitle = "Draft Article",
                    MetaDescription = "Draft Description",
                    Summary = "Draft summary",
                    Content = "Draft content",
                    CategoryId = 1,
                    AuthorId = 2,
                    IsPublished = false,
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Article
                {
                    Id = 4,
                    Title = "Deleted Article",
                    Slug = "deleted-article",
                    MetaTitle = "Deleted Article",
                    MetaDescription = "Deleted Description",
                    Summary = "Deleted summary",
                    Content = "Deleted content",
                    CategoryId = 1,
                    AuthorId = 1,
                    IsPublished = true,
                    IsDeleted = true,
                    PublishedAt = DateTime.UtcNow.AddDays(-3),
                    ViewCount = 200,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            );

            context.SaveChanges();
        }
    }

}