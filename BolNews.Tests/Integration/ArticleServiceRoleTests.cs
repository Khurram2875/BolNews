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
    public class ArticleServiceRoleTests : IDisposable
    {
        private readonly ArticleService _service;
        private readonly IMemoryCache _cache;

        public ArticleServiceRoleTests()
        {
            var context = TestDbContextFactory.CreateWithSeed();
            _cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
            var repo = new ArticleRepository(context);
            var scoringService = new Mock<IArticleScoringService>().Object;

            var revisionService = new Mock<IArticleRevisionService>().Object;
            var notificationService = new Mock<INotificationService>().Object;
            var workflowTransitionService = new Mock<IWorkflowTransitionService>().Object;
            var editorialAssignmentService = new Mock<IEditorialAssignmentService>().Object;
            var authorService = new Mock<IAuthorService>();
            var slaService = new Mock<ISlaService>().Object;
            authorService
              .Setup(x => x.GetAuthorByUserId(It.IsAny<string>()))
              .ReturnsAsync(new Application.DTOs.AuthorDto
              {
                  Id = 1,
                  UserId = "test-user-id",
                  Name = "Test Author"
              });

            _service = new ArticleService(repo, _cache, scoringService, revisionService, authorService.Object, notificationService, workflowTransitionService, editorialAssignmentService, slaService);
        }

        // ── CanEditAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task CanEdit_AdminRole_CanEditAnyArticle()
        {
            var result = await _service.CanEditAsync(1, "some-other-user", new[] { Roles.Admin });
            result.Should().BeTrue("Admins can edit any article");
        }

        [Fact]
        public async Task CanEdit_EditorRole_CanEditAnyArticle()
        {
            var result = await _service.CanEditAsync(2, "some-other-user", new[] { Roles.Editor });
            result.Should().BeTrue("Editors can edit any article");
        }

        [Fact]
        public async Task CanEdit_SubEditorRole_CanEditAnyArticle()
        {
            var result = await _service.CanEditAsync(1, "some-other-user", new[] { Roles.SubEditor });
            result.Should().BeTrue("SubEditors can edit any article");
        }

        [Fact]
        public async Task CanEdit_AuthorRole_CanEditOwnArticle()
        {
            var result = await _service.CanEditAsync(1, "user-author-1", new[] { Roles.Author });
            result.Should().BeTrue("Authors can edit their own articles");
        }

        [Fact]
        public async Task CanEdit_AuthorRole_CannotEditOtherAuthorsArticle()
        {
            var result = await _service.CanEditAsync(1, "user-author-2", new[] { Roles.Author });
            result.Should().BeFalse("Authors cannot edit other authors' articles");
        }

        [Fact]
        public async Task CanEdit_NoRole_ReturnsFalse()
        {
            var result = await _service.CanEditAsync(1, "user-author-1", new List<string>());
            result.Should().BeFalse("Users with no role cannot edit");
        }

        [Fact]
        public async Task CanEdit_NonExistentArticle_ReturnsFalse()
        {
            // For Author role, a non-existent article must return false.
            // Admin/Editor/SubEditor bypass the article existence check by design —
            // they are trusted to operate on any article ID (e.g. from a route param).
            var result = await _service.CanEditAsync(999, "user-author-1", new[] { Roles.Author });
            result.Should().BeFalse("Non-existent article should return false for Author role");
        }

        // ── CanDeleteAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task CanDelete_AdminRole_CanDeleteAnyArticle()
        {
            var result = await _service.CanDeleteAsync(1, "other-user", new[] { Roles.Admin });
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanDelete_SubEditorRole_CannotDelete()
        {
            var result = await _service.CanDeleteAsync(1, "other-user", new[] { Roles.SubEditor });
            result.Should().BeFalse("SubEditors do not have delete permission");
        }

        [Fact]
        public async Task CanDelete_AuthorRole_CanDeleteOwnArticle()
        {
            var result = await _service.CanDeleteAsync(1, "user-author-1", new[] { Roles.Author });
            result.Should().BeTrue("Authors can delete their own articles");
        }

        [Fact]
        public async Task CanDelete_AuthorRole_CannotDeleteOtherAuthorsArticle()
        {
            var result = await _service.CanDeleteAsync(1, "user-author-2", new[] { Roles.Author });
            result.Should().BeFalse("Authors cannot delete other authors' articles");
        }

        // ── GetAllAsync role filtering ────────────────────────────────────────

        [Fact]
        public async Task GetAll_AdminRole_ReturnsAllNonDeletedArticles()
        {
            var result = await _service.GetAllAsync("any-user", new[] { Roles.Admin });
            result.Should().HaveCount(3, "Admin sees all non-deleted articles");
        }

        [Fact]
        public async Task GetAll_AuthorRole_ReturnsOnlyOwnArticles()
        {
            var result = await _service.GetAllAsync("user-author-1", new[] { Roles.Author });
            result.Should().HaveCount(2, "Author only sees their own articles");
            result.Should().OnlyContain(a => a.AuthorId == 1);
        }

        [Fact]
        public async Task GetAll_AuthorRoleNoAuthorRecord_ReturnsEmpty()
        {
            var result = await _service.GetAllAsync("user-with-no-author-record", new[] { Roles.Author });
            result.Should().BeEmpty("No author record means no articles");
        }

        public void Dispose() => _cache.Dispose();
    }
}
