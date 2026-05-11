using BolNews.Persistence.Context;
using BolNews.Persistence.Repositories;

namespace BolNews.Tests.Helpers
{
    /// <summary>
    /// Creates repository instances backed by the InMemory AppDbContext.
    /// Use this in tests instead of passing AppDbContext directly to services,
    /// since services now depend on repository interfaces, not AppDbContext.
    /// </summary>
    public static class TestRepositoryFactory
    {
        public static (
            ArticleRepository Article,
            AuthorRepository Author,
            CategoryRepository Category,
            AnalyticsRepository Analytics)
        CreateAll(AppDbContext context) => (
            new ArticleRepository(context),
            new AuthorRepository(context),
            new CategoryRepository(context),
            new AnalyticsRepository(context)
        );
    }
}