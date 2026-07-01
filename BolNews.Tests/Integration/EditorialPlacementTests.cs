using BolNews.Application.Interfaces;
using BolNews.Application.Interfaces.Scoring;
using BolNews.Application.Common;
using BolNews.Application.Services;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Persistence.Repositories;
using BolNews.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace BolNews.Tests.Integration;

public class EditorialPlacementTests
{
    [Fact]
    public async Task GetTopStoryAsync_ReturnsPinnedTopStoryBeforeLatestPublished()
    {
        await using var context = TestDbContextFactory.CreateWithSeed();
        var articleRepository = new ArticleRepository(context);
        var placementRepository = new EditorialPlacementRepository(context);
        var service = CreateArticleService(articleRepository, placementRepository);

        context.EditorialPlacements.Add(new EditorialPlacement
        {
            PlacementKey = EditorialPlacementKeys.HomepageTopStory,
            ArticleId = 2,
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var topStory = await service.GetTopStoryAsync();

        Assert.NotNull(topStory);
        Assert.Equal(2, topStory.Id);
    }

    [Fact]
    public async Task GetSecondaryStoriesAsync_ReturnsPinnedStoriesThenFallbackWithoutDuplicates()
    {
        await using var context = TestDbContextFactory.CreateWithSeed();
        var articleRepository = new ArticleRepository(context);
        var placementRepository = new EditorialPlacementRepository(context);
        var service = CreateArticleService(articleRepository, placementRepository);

        context.EditorialPlacements.AddRange(
            new EditorialPlacement
            {
                PlacementKey = EditorialPlacementKeys.HomepageTopStory,
                ArticleId = 1,
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow
            },
            new EditorialPlacement
            {
                PlacementKey = EditorialPlacementKeys.HomepageSecondaryStory,
                ArticleId = 2,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var secondaryStories = await service.GetSecondaryStoriesAsync(2);

        Assert.Single(secondaryStories);
        Assert.Equal(2, secondaryStories[0].Id);
        Assert.DoesNotContain(secondaryStories, x => x.Id == 1);
    }

    [Fact]
    public async Task PinTopStoryAsync_RemovesMatchingSecondaryPlacementAndInvalidatesHomepage()
    {
        await using var context = TestDbContextFactory.CreateWithSeed();
        var articleRepository = new ArticleRepository(context);
        var placementRepository = new EditorialPlacementRepository(context);
        var cacheService = new Mock<ICacheService>();
        var service = new EditorialPlacementService(placementRepository, articleRepository, cacheService.Object);

        context.EditorialPlacements.Add(new EditorialPlacement
        {
            PlacementKey = EditorialPlacementKeys.HomepageSecondaryStory,
            ArticleId = 2,
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        await service.PinTopStoryAsync(2, "editor-user", new List<string> { Roles.Editor });

        var topStory = await placementRepository.GetActivePlacementAsync(EditorialPlacementKeys.HomepageTopStory);
        var secondaryStory = await placementRepository.GetActivePlacementByArticleAsync(
            EditorialPlacementKeys.HomepageSecondaryStory,
            2);

        Assert.NotNull(topStory);
        Assert.Equal(2, topStory.ArticleId);
        Assert.Null(secondaryStory);
        cacheService.Verify(x => x.Remove(CacheKeys.HomePage), Times.Once);
    }

    [Fact]
    public async Task PinSecondaryStoryAsync_RejectsAuthorRole()
    {
        await using var context = TestDbContextFactory.CreateWithSeed();
        var articleRepository = new ArticleRepository(context);
        var placementRepository = new EditorialPlacementRepository(context);
        var cacheService = new Mock<ICacheService>();
        var service = new EditorialPlacementService(placementRepository, articleRepository, cacheService.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.PinSecondaryStoryAsync(1, "author-user", new List<string> { Roles.Author }));
    }

    private static ArticleService CreateArticleService(
        ArticleRepository articleRepository,
        EditorialPlacementRepository placementRepository)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());

        return new ArticleService(
            articleRepository,
            cache,
            Mock.Of<IArticleScoringService>(),
            Mock.Of<IArticleRevisionService>(),
            Mock.Of<IAuthorService>(),
            Mock.Of<INotificationService>(),
            Mock.Of<IWorkflowTransitionService>(),
            Mock.Of<IEditorialAssignmentService>(),
            Mock.Of<ISlaService>(),
            placementRepository);
    }
}
