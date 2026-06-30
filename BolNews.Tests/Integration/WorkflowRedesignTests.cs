using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Application.Interfaces.Scoring;
using BolNews.Application.Services;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;
using BolNews.Persistence.Context;
using BolNews.Persistence.Repositories;
using BolNews.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BolNews.Tests.Integration
{
    public class WorkflowRedesignTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ArticleRepository _articleRepository;
        private readonly IMemoryCache _cache;
        private readonly Mock<IArticleScoringService> _scoringService = new();
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly Mock<IAuthorService> _authorService = new();
        private readonly ArticleService _articleService;

        public WorkflowRedesignTests()
        {
            _context = TestDbContextFactory.CreateWithSeed();
            _articleRepository = new ArticleRepository(_context);
            _cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));

            var revisionRepository = new ArticleRevisionRepository(_context);
            var revisionService = new ArticleRevisionService(revisionRepository);
            var workflowTransitionService = new Mock<IWorkflowTransitionService>().Object;
            var editorialAssignmentService = new Mock<IEditorialAssignmentService>().Object;
            var slaService = new SlaService();

            _authorService
                .Setup(x => x.GetAuthorByUserId("user-author-1"))
                .ReturnsAsync(new AuthorDto { Id = 1, UserId = "user-author-1", Name = "Test Author One" });

            _articleService = new ArticleService(
                _articleRepository,
                _cache,
                _scoringService.Object,
                revisionService,
                _authorService.Object,
                _notificationService.Object,
                workflowTransitionService,
                editorialAssignmentService,
                slaService);
        }

        [Fact]
        public async Task Author_CreatePublishedArticle_PublishesDirectly()
        {
            var id = await _articleService.CreateAsync(
                NewArticleDto("author-direct-publish", authorId: 1, isPublished: true),
                "user-author-1",
                new[] { Roles.Author });

            var article = await _articleRepository.FindByIdAsync(id);

            article!.WorkflowStatus.Should().Be(ArticleWorkflowStatus.Published);
            article.IsPublished.Should().BeTrue();
            article.PublishedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Author_EditPublishedArticleByAnotherAuthor_PreservesPublishedStateAndCreatesRevision()
        {
            var article = await _articleRepository.FindByIdAsync(1);
            article!.WorkflowStatus = ArticleWorkflowStatus.Published;
            article.EditorialPriority = 3;
            await _articleRepository.SaveChangesAsync();

            var originalPublishedAt = article.PublishedAt;

            await _articleService.UpdateAsync(
                ExistingArticleDto(article, title: "Updated by another author", isPublished: false, editorialPriority: 0),
                "user-author-2",
                new[] { Roles.Author },
                "Published article update");

            var updated = await _articleRepository.FindByIdAsync(1);

            updated!.WorkflowStatus.Should().Be(ArticleWorkflowStatus.Published);
            updated.IsPublished.Should().BeTrue();
            updated.PublishedAt.Should().Be(originalPublishedAt);
            updated.EditorialPriority.Should().Be(3);
            _context.ArticleRevisions.Where(x => x.ArticleId == 1).Should().ContainSingle();
            _notificationService.Verify(
                x => x.NotifyAsync(
                    "user-author-1",
                    "Article Updated",
                    It.IsAny<string>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task SubEditor_EditPublishedArticle_PreservesPriorityAndPublishedState()
        {
            var article = await _articleRepository.FindByIdAsync(2);
            article!.WorkflowStatus = ArticleWorkflowStatus.Published;
            article.EditorialPriority = 2;
            await _articleRepository.SaveChangesAsync();

            await _articleService.UpdateAsync(
                ExistingArticleDto(article, title: "Updated by subeditor", isPublished: false, editorialPriority: 0),
                "subeditor-user",
                new[] { Roles.SubEditor },
                "SubEditor published update");

            var updated = await _articleRepository.FindByIdAsync(2);

            updated!.WorkflowStatus.Should().Be(ArticleWorkflowStatus.Published);
            updated.IsPublished.Should().BeTrue();
            updated.PublishedAt.Should().NotBeNull();
            updated.EditorialPriority.Should().Be(2);
        }

        [Fact]
        public async Task SubEditor_CreateArticleWithSelectedAuthor_UsesSelectedAuthor()
        {
            var id = await _articleService.CreateAsync(
                NewArticleDto("subeditor-selected-author", authorId: 2, isPublished: true),
                "subeditor-user",
                new[] { Roles.SubEditor });

            var article = await _articleRepository.FindByIdAsync(id);

            article!.AuthorId.Should().Be(2);
            article.WorkflowStatus.Should().Be(ArticleWorkflowStatus.Published);
        }

        [Fact]
        public async Task Editor_CanChangePriority()
        {
            var article = await _articleRepository.FindByIdAsync(3);

            await _articleService.UpdateAsync(
                ExistingArticleDto(article!, title: "Editor priority update", isPublished: false, editorialPriority: 2),
                "editor-user",
                new[] { Roles.Editor },
                "Priority update");

            var updated = await _articleRepository.FindByIdAsync(3);

            updated!.EditorialPriority.Should().Be(2);
        }

        [Fact]
        public async Task UnderReview_CanBeApprovedWithoutFactCheck()
        {
            var article = NewWorkflowArticle(ArticleWorkflowStatus.UnderReview);
            var service = CreateWorkflowTransitionService();

            await service.ExecuteTransitionAsync(
                article,
                ArticleWorkflowStatus.Approved,
                "subeditor-user",
                new[] { Roles.SubEditor });

            article.WorkflowStatus.Should().Be(ArticleWorkflowStatus.Approved);
            article.ApprovedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Draft_CanPublishDirectlyWithoutSlaTracking()
        {
            var article = NewWorkflowArticle(ArticleWorkflowStatus.Draft);
            var service = CreateWorkflowTransitionService();

            await service.ExecuteTransitionAsync(
                article,
                ArticleWorkflowStatus.Published,
                "user-author-1",
                new[] { Roles.Author });

            article.WorkflowStatus.Should().Be(ArticleWorkflowStatus.Published);
            article.IsPublished.Should().BeTrue();
            new SlaService().Evaluate(article).IsTracked.Should().BeFalse();
        }

        [Fact]
        public void FactCheckPending_StartsFactCheckSla()
        {
            var article = new Article
            {
                WorkflowStatus = ArticleWorkflowStatus.FactCheckPending,
                FactCheckStartedAt = DateTime.UtcNow.AddHours(-1)
            };

            var result = new SlaService().Evaluate(article);

            result.IsTracked.Should().BeTrue();
            result.StatusLabel.Should().Be("Fact Check");
        }

        [Fact]
        public async Task PublishTransition_NotifiesAuthor()
        {
            var article = NewWorkflowArticle(ArticleWorkflowStatus.Approved);
            var service = CreateWorkflowTransitionService();

            await service.ExecuteTransitionAsync(
                article,
                ArticleWorkflowStatus.Published,
                "editor-user",
                new[] { Roles.Editor });

            _notificationService.Verify(
                x => x.NotifyAsync(
                    "user-author-1",
                    "Article Published",
                    It.IsAny<string>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        private WorkflowTransitionService CreateWorkflowTransitionService()
        {
            var revisionService = new Mock<IArticleRevisionService>();
            var cacheService = new Mock<ICacheService>();
            var logger = new Mock<ILogger<WorkflowTransitionService>>();

            return new WorkflowTransitionService(
                revisionService.Object,
                _notificationService.Object,
                _scoringService.Object,
                cacheService.Object,
                logger.Object);
        }

        private static Article NewWorkflowArticle(ArticleWorkflowStatus status)
        {
            return new Article
            {
                Id = 99,
                Title = "Workflow article",
                Slug = "workflow-article",
                MetaTitle = "Workflow article",
                MetaDescription = "Workflow article",
                Summary = "Summary",
                Content = "Content",
                WorkflowStatus = status,
                Author = new Author
                {
                    UserId = "user-author-1",
                    Name = "Author",
                    Slug = "author",
                    Bio = "Bio"
                },
                Category = new Category
                {
                    Name = "Technology",
                    Slug = "technology"
                }
            };
        }

        private static ArticleDto NewArticleDto(string slug, int authorId, bool isPublished)
        {
            return new ArticleDto
            {
                Title = slug,
                Slug = slug,
                MetaTitle = slug,
                MetaDescription = slug,
                Summary = "Summary",
                Content = "Content",
                CategoryId = 1,
                AuthorId = authorId,
                IsPublished = isPublished
            };
        }

        private static ArticleDto ExistingArticleDto(
            Article article,
            string title,
            bool isPublished,
            int editorialPriority)
        {
            return new ArticleDto
            {
                Id = article.Id,
                Title = title,
                Slug = article.Slug,
                MetaTitle = article.MetaTitle,
                MetaDescription = article.MetaDescription,
                Summary = article.Summary,
                Content = article.Content,
                CategoryId = article.CategoryId,
                AuthorId = article.AuthorId,
                IsPublished = isPublished,
                PublishedAt = article.PublishedAt,
                EditorialPriority = editorialPriority,
                IsFactChecked = article.IsFactChecked,
                IsEditorsPick = article.IsEditorsPick
            };
        }

        public void Dispose()
        {
            _cache.Dispose();
            _context.Dispose();
        }
    }
}
