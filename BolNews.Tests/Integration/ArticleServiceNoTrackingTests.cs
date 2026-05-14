using BolNews.Application.Interfaces;
using BolNews.Application.Interfaces.Scoring;
using BolNews.Application.Services;
using BolNews.Domain.Entities;
using BolNews.Persistence.Repositories;
using BolNews.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace BolNews.Tests.Integration;

public class ArticleServiceNoTrackingTests
{
    [Fact]
    public async Task GetBySlugAsync_ShouldNotTrackEntities()
    {
        var (context, conn) = TestDbContextFactory.CreateSqlite();
        await using (conn)
        await using (context)
        {
            var repo = new ArticleRepository(context);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var scoringServiceMock = new Mock<IArticleScoringService>();
            var revisionServiceMock = new Mock<IArticleRevisionService>();
            var authorServiceMock = new Mock<IAuthorService>();

            var service = new ArticleService(
                repo,
                cache,
                scoringServiceMock.Object,
                revisionServiceMock.Object,
                authorServiceMock.Object
            );

            // SQLite enforces FK constraints — ApplicationUser must exist
            // before Author can reference it via UserId.
            var user = new ApplicationUser
            {
                Id = "u1",
                UserName = "test@test.com",
                Email = "test@test.com",
                FullName = "Test User"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var category = new Category { Name = "Tech", Slug = "tech" };
            var author = new Author
            {
                Name = "Auth",
                Slug = "auth",
                Bio = "bio",
                UserId = "u1",
                User = user
            };
            context.Categories.Add(category);
            context.Authors.Add(author);
            await context.SaveChangesAsync();

            context.Articles.Add(new Article
            {
                Title = "T",
                Slug = "t",
                MetaTitle = "mt",
                MetaDescription = "md",
                Summary = "s",
                Content = "c",
                CategoryId = category.Id,
                AuthorId = author.Id,
                IsPublished = true,
                PublishedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            context.ChangeTracker.Clear();

            var article = await service.GetBySlugAsync("t");

            Assert.NotNull(article);
            Assert.Empty(context.ChangeTracker.Entries());
        }
    }
}
