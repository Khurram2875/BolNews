using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Application.Interfaces.Scoring;
using BolNews.Application.Services;
using BolNews.Domain.Common;
using BolNews.Persistence.Repositories;
using BolNews.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BolNews.Tests.Integration
{
    public class ArticleServiceCrudTests : IDisposable
    {
        private readonly ArticleService _service;
        private readonly IMemoryCache _cache;

        public ArticleServiceCrudTests()
        {
            var context = TestDbContextFactory.CreateWithSeed();
            _cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
            var repo = new ArticleRepository(context);

            // Create a mock or fake IArticleScoringService for testing
            var scoringService = new Mock<IArticleScoringService>().Object;
            var revisionService = new Mock<IArticleRevisionService>().Object;
            var authorService = new Mock<IAuthorService>();
            authorService
              .Setup(x => x.GetAuthorByUserId(It.IsAny<string>()))
              .ReturnsAsync(new Application.DTOs.AuthorDto
              {
                  Id = 1,
                  UserId = "test-user-id",
                  Name = "Test Author"
              });

            _service = new ArticleService(repo,_cache,scoringService,revisionService,authorService.Object);
        }

        // ── GenerateUniqueSlugAsync ───────────────────────────────────────────

        [Fact]
        public async Task GenerateUniqueSlug_NewTitle_ReturnsCleantSlug()
        {
            var slug = await _service.GenerateUniqueSlugAsync("Pakistan Economy 2025");
            slug.Should().Be("pakistan-economy-2025");
        }

        [Fact]
        public async Task GenerateUniqueSlug_DuplicateSlug_AppendsCounter()
        {
            var slug = await _service.GenerateUniqueSlugAsync("First Technology Article");
            slug.Should().Be("first-technology-article-1",
                "a counter suffix must be added when the base slug already exists");
        }

        [Fact]
        public async Task GenerateUniqueSlug_TwoDuplicates_CounterIncrements()
        {
            var slug1 = await _service.GenerateUniqueSlugAsync("Second Technology Article");
            var slug2 = await _service.GenerateUniqueSlugAsync("Second Technology Article");

            slug1.Should().Be("second-technology-article-1");
            slug2.Should().Be("second-technology-article-1",
                "both calls see the same DB state; the second article isn't saved yet");
        }

        // ── CreateAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task Create_ValidDto_ReturnsNewId()
        {
            var dto = new ArticleDto
            {
                Title = "New Test Article",
                Slug = "new-test-article",
                MetaTitle = "New Article Meta",
                MetaDescription = "Meta description for new article",
                Summary = "Summary",
                Content = "Content",
                CategoryId = 1,
                AuthorId = 1,
                IsPublished = false
            };

            var id = await _service.CreateAsync(
    dto,
    "test-user-id",
    new List<string> { Roles.Author });
            id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Create_PublishedArticle_SetsPublishedAt()
        {
            var dto = new ArticleDto
            {
                Title = "Published Article",
                Slug = "published-article-new",
                MetaTitle = "Published Article Meta",
                MetaDescription = "Meta description",
                Summary = "Summary",
                Content = "Content",
                CategoryId = 1,
                AuthorId = 1,
                IsPublished = true,
                PublishedAt = null, // Service should set this automatically
                IsEditorsPick = true,
                IsFactChecked = true,
                EditorialPriority = 5
            };

            var id = await _service.CreateAsync(
    dto,
    "test-user-id",
    new List<string> { Roles.Author });
            var result = await _service.GetByIdAsync(id);

            result!.PublishedAt.Should().NotBeNull();
            result.PublishedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Create_DraftArticle_PublishedAtIsNull()
        {
            var dto = new ArticleDto
            {
                Title = "Draft Article New",
                Slug = "draft-article-new",
                MetaTitle = "Draft Meta",
                MetaDescription = "Draft description",
                Summary = "Summary",
                Content = "Content",
                CategoryId = 1,
                AuthorId = 1,
                IsPublished = false
            };

            var id = await _service.CreateAsync(
    dto,
    "test-user-id",
    new List<string> { Roles.Author });
            var result = await _service.GetByIdAsync(id);

            result!.PublishedAt.Should().BeNull();
        }

        // ── DeleteAsync (soft delete) ─────────────────────────────────────────

        [Fact]
        public async Task Delete_ExistingArticle_SoftDeletesIt()
        {
            await _service.DeleteAsync(2);

            // After soft-delete the article is invisible to all service queries
            var allPublished = await _service.GetAllPublishedAsync();
            allPublished.Should().NotContain(a => a.Id == 2,
                "soft-deleted articles must not appear in published listings");
        }

        // ── GetByAuthorAsync pagination ───────────────────────────────────────

        [Fact]
        public async Task GetByAuthor_Page1_ReturnsCorrectArticles()
        {
            var result = await _service.GetByAuthorAsync(authorId: 1, page: 1, pageSize: 10);
            result.Should().HaveCount(2);
            result.Should().OnlyContain(a => a.AuthorId == 1);
        }

        [Fact]
        public async Task GetByAuthor_PageSizeOne_ReturnsOneArticle()
        {
            var result = await _service.GetByAuthorAsync(authorId: 1, page: 1, pageSize: 1);
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetByAuthor_Page2WithPageSizeOne_ReturnsSecondArticle()
        {
            var page1 = await _service.GetByAuthorAsync(authorId: 1, page: 1, pageSize: 1);
            var page2 = await _service.GetByAuthorAsync(authorId: 1, page: 2, pageSize: 1);

            page1.Single().Id.Should().NotBe(page2.Single().Id,
                "page 1 and page 2 should return different articles");
        }

        // ── GetTrendingAsync scoring ──────────────────────────────────────────

        [Fact]
        public async Task GetTrending_ReturnsHighViewCountArticleFirst()
        {
            var trending = await _service.GetTrendingAsync(count: 5, type: "week");

            trending.Should().NotBeEmpty();
            trending.First().Id.Should().Be(1,
                "highest view count + most recent should rank first");
        }

        [Fact]
        public async Task GetTrending_TypeToday_ExcludesOlderArticles()
        {
            var trending = await _service.GetTrendingAsync(count: 5, type: "today");
            trending.Should().BeEmpty("no articles published within the last 24 hours");
        }

        [Fact]
        public async Task GetTrending_DeletedArticles_AreExcluded()
        {
            var trending = await _service.GetTrendingAsync(count: 10, type: "month");
            trending.Should().NotContain(a => a.Id == 4,
                "soft-deleted articles must never appear in trending results");
        }

        public void Dispose() => _cache.Dispose();
    }
}