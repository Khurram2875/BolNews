using BolNews.Application.Services;
using BolNews.Domain.Common;
using BolNews.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace BolNews.Tests.Integration
{
    /// <summary>
    /// Tests for role-based access control in ArticleService.
    /// Uses EF Core InMemory provider — no live database needed.
    /// </summary>
    public class ArticleServiceRoleTests : IDisposable
    {
        private readonly ArticleService _service;
        private readonly IMemoryCache _cache;

        public ArticleServiceRoleTests()
        {
            var context = TestDbContextFactory.CreateWithSeed();
            _cache      = new MemoryCache(Options.Create(new MemoryCacheOptions()));
            _service    = new ArticleService(context, _cache);
        }

        // ── CanEditAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task CanEdit_AdminRole_CanEditAnyArticle()
        {
            var result = await _service.CanEditAsync(
                articleId: 1,
                userId: "some-other-user",
                roles: new[] { Roles.Admin });

            result.Should().BeTrue("Admins can edit any article");
        }

        [Fact]
        public async Task CanEdit_EditorRole_CanEditAnyArticle()
        {
            var result = await _service.CanEditAsync(
                articleId: 2,
                userId: "some-other-user",
                roles: new[] { Roles.Editor });

            result.Should().BeTrue("Editors can edit any article");
        }

        [Fact]
        public async Task CanEdit_SubEditorRole_CanEditAnyArticle()
        {
            var result = await _service.CanEditAsync(
                articleId: 1,
                userId: "some-other-user",
                roles: new[] { Roles.SubEditor });

            result.Should().BeTrue("SubEditors can edit any article");
        }

        [Fact]
        public async Task CanEdit_AuthorRole_CanEditOwnArticle()
        {
            // Article 1 belongs to author with UserId = "user-author-1"
            var result = await _service.CanEditAsync(
                articleId: 1,
                userId: "user-author-1",
                roles: new[] { Roles.Author });

            result.Should().BeTrue("Authors can edit their own articles");
        }

        [Fact]
        public async Task CanEdit_AuthorRole_CannotEditOtherAuthorsArticle()
        {
            // Article 1 belongs to author-1; author-2 tries to edit it
            var result = await _service.CanEditAsync(
                articleId: 1,
                userId: "user-author-2",
                roles: new[] { Roles.Author });

            result.Should().BeFalse("Authors cannot edit other authors' articles");
        }

        [Fact]
        public async Task CanEdit_NoRole_ReturnsFalse()
        {
            var result = await _service.CanEditAsync(
                articleId: 1,
                userId: "user-author-1",
                roles: new List<string>());

            result.Should().BeFalse("Users with no role cannot edit");
        }

        [Fact]
        public async Task CanEdit_NonExistentArticle_ReturnsFalse()
        {
            var result = await _service.CanEditAsync(
                articleId: 999,
                userId: "user-author-1",
                roles: new[] { Roles.Admin });

            result.Should().BeFalse("Non-existent article should return false");
        }

        // ── CanDeleteAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task CanDelete_AdminRole_CanDeleteAnyArticle()
        {
            var result = await _service.CanDeleteAsync(
                articleId: 1,
                userId: "other-user",
                roles: new[] { Roles.Admin });

            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanDelete_SubEditorRole_CannotDelete()
        {
            // SubEditors can edit but NOT delete — important RBAC boundary
            var result = await _service.CanDeleteAsync(
                articleId: 1,
                userId: "other-user",
                roles: new[] { Roles.SubEditor });

            result.Should().BeFalse("SubEditors do not have delete permission");
        }

        [Fact]
        public async Task CanDelete_AuthorRole_CanDeleteOwnArticle()
        {
            var result = await _service.CanDeleteAsync(
                articleId: 1,
                userId: "user-author-1",
                roles: new[] { Roles.Author });

            result.Should().BeTrue("Authors can delete their own articles");
        }

        [Fact]
        public async Task CanDelete_AuthorRole_CannotDeleteOtherAuthorsArticle()
        {
            var result = await _service.CanDeleteAsync(
                articleId: 1,
                userId: "user-author-2",
                roles: new[] { Roles.Author });

            result.Should().BeFalse("Authors cannot delete other authors' articles");
        }

        // ── GetAllAsync role filtering ────────────────────────────────────────

        [Fact]
        public async Task GetAll_AdminRole_ReturnsAllNonDeletedArticles()
        {
            var result = await _service.GetAllAsync("any-user", new[] { Roles.Admin });

            // Seed has 4 articles; 1 is soft-deleted — Admin should see 3
            result.Should().HaveCount(3, "Admin sees all non-deleted articles");
        }

        [Fact]
        public async Task GetAll_AuthorRole_ReturnsOnlyOwnArticles()
        {
            // Author 1 owns articles 1 and 2 (not deleted); article 3 belongs to author 2
            var result = await _service.GetAllAsync("user-author-1", new[] { Roles.Author });

            result.Should().HaveCount(2, "Author only sees their own articles");
            result.Should().OnlyContain(a => a.AuthorId == 1);
        }

        [Fact]
        public async Task GetAll_AuthorRoleNoAuthorRecord_ReturnsEmpty()
        {
            // A user who has the Author role but no Author entity in the DB
            var result = await _service.GetAllAsync("user-with-no-author-record", new[] { Roles.Author });

            result.Should().BeEmpty("No author record means no articles");
        }

        public void Dispose() => _cache.Dispose();
    }
}
