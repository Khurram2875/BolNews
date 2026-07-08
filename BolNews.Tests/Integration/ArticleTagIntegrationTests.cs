using BolNews.Application.Services;
using BolNews.Domain.Entities;
using BolNews.Persistence.Repositories;
using BolNews.Tests.Helpers;
using Xunit;

namespace BolNews.Tests.Integration
{
    public class ArticleTagIntegrationTests
    {
        [Fact]
        public async Task ReplaceArticleTagsAsync_PersistsArticleTags()
        {
            await using var context = TestDbContextFactory.CreateWithSeed();
            var repository = new TagRepository(context);
            var service = new TagService(repository);

            await service.ReplaceArticleTagsAsync(1, "Politics, Economy", "editor-user");

            var tags = await service.GetArticleTagsAsync(1);

            Assert.Equal(new[] { "Economy", "Politics" }, tags.Select(x => x.Name).ToArray());
        }

        [Fact]
        public async Task ReplaceFeaturedImageTagsAsync_PersistsMetadataAndImageTags()
        {
            await using var context = TestDbContextFactory.CreateWithSeed();
            var repository = new TagRepository(context);
            var service = new TagService(repository);

            await service.ReplaceFeaturedImageTagsAsync(
                1,
                "Flood, Rescue",
                "Rescue workers in a flooded street",
                "Flood response in Karachi",
                "Bol News",
                "editor-user");

            var metadata = await repository.GetFeaturedImageMetadataAsync(1);
            var tags = await service.GetFeaturedImageTagsAsync(1);

            Assert.NotNull(metadata);
            Assert.Equal("Rescue workers in a flooded street", metadata!.AltText);
            Assert.Equal("Flood response in Karachi", metadata.Caption);
            Assert.Equal("Bol News", metadata.Credit);
            Assert.Equal(new[] { "Flood", "Rescue" }, tags.Select(x => x.Name).ToArray());
        }

        [Fact]
        public async Task GetByTagSlugAsync_ReturnsPublishedNonDeletedArticlesOnly()
        {
            await using var context = TestDbContextFactory.CreateWithSeed();
            var tag = new Tag
            {
                Id = 20,
                Name = "Politics",
                NormalizedName = "POLITICS",
                Slug = "politics",
                CreatedAt = DateTime.UtcNow
            };

            context.Tags.Add(tag);
            context.ArticleTags.AddRange(
                new ArticleTag { ArticleId = 1, TagId = tag.Id, CreatedAt = DateTime.UtcNow },
                new ArticleTag { ArticleId = 3, TagId = tag.Id, CreatedAt = DateTime.UtcNow },
                new ArticleTag { ArticleId = 4, TagId = tag.Id, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var repository = new ArticleRepository(context);

            var articles = await repository.GetByTagSlugAsync("politics", 1, 10);

            Assert.Single(articles);
            Assert.Equal(1, articles[0].Id);
        }
    }
}
