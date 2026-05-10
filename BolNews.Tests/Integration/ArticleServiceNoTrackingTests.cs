using BolNews.Application.Services;
using BolNews.Domain.Entities;
using BolNews.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace BolNews.Tests.Integration;

public class ArticleServiceNoTrackingTests
{
    [Fact]
    public async Task GetBySlugAsync_ShouldNotTrackEntities()
    {
        await using var context = TestDbContextFactory.Create();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new ArticleService(context, cache);

        var category = new Category { Name = "Tech", Slug = "tech" };
        var author = new Author { Name = "Auth", Slug = "auth", Bio = "bio", UserId = "u1" };
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
