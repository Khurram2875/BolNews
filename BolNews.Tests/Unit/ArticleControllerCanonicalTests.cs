using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Controllers;
using BolNews.Web.Interfaces;
using BolNews.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BolNews.Tests.Unit;

public class ArticleControllerCanonicalTests
{
    [Fact]
    public async Task Details_WhenCategorySlugIsNonCanonical_ShouldRedirectWithoutTracking()
    {
        var service = new Mock<IArticlePageService>();
        service.Setup(s => s.BuildDetailsPageAsync("my-article"))
            .ReturnsAsync(new ArticleDetailsPageVM
            {
                Article = new PublicArticleVM
                {
                    Id = 42,
                    Slug = "my-article",
                    CategorySlug = "canonical-category",
                    Title = "My Article",
                    MetaTitle = "My Article",
                    Summary = "summary"
                },
                RelatedArticles = new List<PublicArticleVM>(),
                BaseUrl = "https://example.com"
            });

        var controller = new ArticleController(service.Object, CreateCacheService());

        var result = await controller.Details("wrong-category", "my-article");

        var redirect = Assert.IsType<RedirectToRouteResult>(result);
        Assert.True(redirect.Permanent);
        Assert.Equal("articleDetails", redirect.RouteName);
        Assert.Equal("canonical-category", redirect.RouteValues?["categorySlug"]);
        Assert.Equal("my-article", redirect.RouteValues?["slug"]);
        service.Verify(s => s.TrackArticleEngagementAsync(It.IsAny<int>(), It.IsAny<ISession>()), Times.Never);
    }

    [Fact]
    public async Task Details_WhenCategorySlugIsCanonical_ShouldTrackEngagementOnce()
    {
        var service = new Mock<IArticlePageService>();
        service.Setup(s => s.BuildDetailsPageAsync("my-article"))
            .ReturnsAsync(new ArticleDetailsPageVM
            {
                Article = new PublicArticleVM
                {
                    Id = 42,
                    Slug = "my-article",
                    CategorySlug = "canonical-category",
                    Title = "My Article",
                    MetaTitle = "My Article",
                    Summary = "summary"
                },
                RelatedArticles = new List<PublicArticleVM>(),
                BaseUrl = "https://example.com"
            });

        var controller = new ArticleController(service.Object, CreateCacheService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // minimal session mock; method is only forwarded to service
        var session = new Mock<ISession>();
        controller.HttpContext.Features.Set<ISessionFeature>(new SessionFeature { Session = session.Object });

        var result = await controller.Details("canonical-category", "my-article");

        Assert.IsType<ViewResult>(result);
        Assert.Equal("/canonical-category/my-article", controller.ViewBag.CanonicalUrl);
        service.Verify(s => s.TrackArticleEngagementAsync(42, It.IsAny<ISession>()), Times.Once);
    }

    [Fact]
    public void LegacyDetails_ShouldRedirectPermanentlyToRootLevelArticleRoute()
    {
        var service = new Mock<IArticlePageService>();
        var controller = new ArticleController(service.Object, CreateCacheService());

        var result = controller.LegacyDetails("canonical-category", "my-article");

        var redirect = Assert.IsType<RedirectToRouteResult>(result);
        Assert.True(redirect.Permanent);
        Assert.Equal("articleDetails", redirect.RouteName);
        Assert.Equal("canonical-category", redirect.RouteValues?["categorySlug"]);
        Assert.Equal("my-article", redirect.RouteValues?["slug"]);
    }

    private sealed class SessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = default!;
    }

    private static ICacheService CreateCacheService()
    {
        var cacheService = new Mock<ICacheService>();
        cacheService.Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<ArticleDetailsPageVM?>>>(),
                It.IsAny<int>()))
            .Returns<string, Func<Task<ArticleDetailsPageVM?>>, int>((_, factory, _) => factory());

        return cacheService.Object;
    }
}
