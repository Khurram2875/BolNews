using BolNews.Domain.Entities;
using BolNews.Domain.Enums;
using BolNews.Persistence.Repositories;
using BolNews.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace BolNews.Tests.Integration;

public class MediaAssetRepositoryTests
{
    [Fact]
    public async Task SearchAsync_FindsMediaByCaptionTagAndFeaturedArticle()
    {
        await using var context = TestDbContextFactory.Create();
        var tag = new Tag { Name = "Election", NormalizedName = "ELECTION", Slug = "election", CreatedAt = DateTime.UtcNow };
        var asset = new MediaAsset { MediaType = MediaType.Image, Url = "/uploads/media/1/xl.webp", Caption = "Campaign rally", CreatedAt = DateTime.UtcNow };
        context.Tags.Add(tag); context.MediaAssets.Add(asset); await context.SaveChangesAsync();
        context.MediaAssetTags.Add(new MediaAssetTag { MediaAssetId = asset.Id, TagId = tag.Id });
        var article = new Article { Title = "Election update", Slug = "election-update", MetaTitle = "Election update", MetaDescription = "Description", Summary = "Summary", Content = "Content", CategoryId = 1, AuthorId = 1, FeaturedMediaId = asset.Id, CreatedAt = DateTime.UtcNow };
        context.Categories.Add(new Category { Id = 1, Name = "News", Slug = "news", CreatedAt = DateTime.UtcNow });
        context.Authors.Add(new Author { Id = 1, UserId = "test", Name = "Test", Slug = "test", Bio = "Test bio", CreatedAt = DateTime.UtcNow });
        context.Articles.Add(article); await context.SaveChangesAsync();
        var repository = new MediaAssetRepository(context);
        (await repository.SearchAsync("Election", null, null, null, null)).Should().ContainSingle(x => x.Id == asset.Id);
        (await repository.SearchAsync("rally", null, null, null, null)).Should().ContainSingle(x => x.Id == asset.Id);
        (await repository.SearchAsync(null, MediaType.Image, null, null, article.Id)).Should().ContainSingle(x => x.Id == asset.Id);
    }
}
