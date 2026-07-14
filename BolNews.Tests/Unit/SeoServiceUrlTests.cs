using System.Text.Json;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Services;
using Xunit;

namespace BolNews.Tests.Unit;

public class SeoServiceUrlTests
{
    [Fact]
    public void BuildArticleSchema_ShouldUseRootLevelArticleUrl()
    {
        var service = new SeoService();
        var article = CreateArticle();

        using var json = JsonDocument.Parse(service.BuildArticleSchema(article, "https://example.com"));

        Assert.Equal(
            "https://example.com/politics/my-article",
            json.RootElement.GetProperty("mainEntityOfPage").GetString());
    }

    [Fact]
    public void BuildBreadcrumb_ShouldUseRootLevelCategoryAndArticleUrls()
    {
        var service = new SeoService();
        var article = CreateArticle();

        using var json = JsonDocument.Parse(service.BuildBreadcrumb(article, "https://example.com"));
        var items = json.RootElement.GetProperty("itemListElement");

        Assert.Equal("https://example.com/politics", items[1].GetProperty("item").GetString());
        Assert.Equal("https://example.com/politics/my-article", items[2].GetProperty("item").GetString());
    }

    private static PublicArticleVM CreateArticle()
        => new()
        {
            Title = "My Article",
            Slug = "my-article",
            MetaDescription = "summary",
            CategoryName = "Politics",
            CategorySlug = "politics",
            AuthorName = "Reporter",
            AuthorSlug = "reporter",
            FeaturedImageXl = "/image.jpg",
            PublishedAt = DateTime.UtcNow
        };
}
