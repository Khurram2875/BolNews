namespace BolNews.Web.Interfaces
{
    public interface ISitemapService
    {
        Task<string> GenerateSitemapIndexAsync();
        Task<string> GenerateArticleSitemapAsync();
        Task<string> GenerateCategorySitemapAsync();
        Task<string> GenerateNewsSitemapAsync();
        Task<string> GenerateAuthorSitemapAsync();
        Task<string> GenerateTagSitemapAsync();
    }
}
