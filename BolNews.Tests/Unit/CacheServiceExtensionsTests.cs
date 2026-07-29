using BolNews.Application.Common;
using BolNews.Application.Interfaces;
using Moq;
using Xunit;

namespace BolNews.Tests.Unit;

public class CacheServiceExtensionsTests
{
    [Fact]
    public void InvalidateHomePage_RemovesAllHomepageVariants()
    {
        var cache = new Mock<ICacheService>();
        cache.Object.InvalidateHomePage();
        cache.Verify(x => x.Remove(CacheKeys.HomePage), Times.Once);
        cache.Verify(x => x.Remove(CacheKeys.HomePageIndex), Times.Once);
        cache.Verify(x => x.Remove(CacheKeys.HomePageIndex1), Times.Once);
    }
}
