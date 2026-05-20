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

public class ArticleServiceCategoryBatchingTests
{
    [Fact]
    public async Task GetArticlesForCategoriesAsync_ShouldReturnTopNPerCategory()
    {
        await using var context = TestDbContextFactory.Create();
        var repo = new ArticleRepository(context);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var scoringServiceMock = new Mock<IArticleScoringService>(); // Mock the required dependency
        var revisionServiceMock = new Mock<IArticleRevisionService>();
        var authorServiceMock = new Mock<IAuthorService>();
        var notificationServiceMock = new Mock<INotificationService>();
        var workflowTransitionServiceMock = new Mock<IWorkflowTransitionService>();
        var editorialAssignmentServiceMock = new Mock<IEditorialAssignmentService>();
        var slaServiceMock = new Mock<ISlaService>();


        var service = new ArticleService(
            repo,
            cache,
            scoringServiceMock.Object,
            revisionServiceMock.Object,
            authorServiceMock.Object,
            notificationServiceMock.Object,
            workflowTransitionServiceMock.Object,
            editorialAssignmentServiceMock.Object,
            slaServiceMock.Object

        );

        var category1 = new Category { Name = "C1", Slug = "c1" };
        var category2 = new Category { Name = "C2", Slug = "c2" };
        context.Categories.AddRange(category1, category2);

        var author = new Author { Name = "A", Slug = "a", Bio = "b", UserId = "u1" };
        context.Authors.Add(author);
        await context.SaveChangesAsync();

        for (var i = 0; i < 10; i++)
        {
            context.Articles.Add(new Article
            {
                Title = $"C1-{i}",
                Slug = $"c1-{i}",
                Summary = "s",
                Content = "c",
                MetaTitle = "m",
                MetaDescription = "d",
                CategoryId = category1.Id,
                AuthorId = author.Id,
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        for (var i = 0; i < 3; i++)
        {
            context.Articles.Add(new Article
            {
                Title = $"C2-{i}",
                Slug = $"c2-{i}",
                Summary = "s",
                Content = "c",
                MetaTitle = "m",
                MetaDescription = "d",
                CategoryId = category2.Id,
                AuthorId = author.Id,
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await context.SaveChangesAsync();

        var result = await service.GetArticlesForCategoriesAsync(
            new List<int> { category1.Id, category2.Id }, 2);

        Assert.Equal(2, result[category1.Id].Count);
        Assert.Equal(2, result[category2.Id].Count);
        Assert.StartsWith("C1-", result[category1.Id][0].Title);
        Assert.StartsWith("C2-", result[category2.Id][0].Title);
    }
}
