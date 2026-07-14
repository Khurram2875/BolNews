namespace BolNews.Web.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static void MapAppRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute(
            name: "authorDetails",
            pattern: "author/{authorSlug}",
            defaults: new { controller = "Author", action = "Details" }
        );

        endpoints.MapControllerRoute(
            name: "tagDetails",
            pattern: "tag/{slug}",
            defaults: new { controller = "Tag", action = "Details" }
        );

        endpoints.MapControllerRoute(
            name: "search",
            pattern: "search",
            defaults: new { controller = "Search", action = "Index" }
        );

        endpoints.MapControllerRoute(
            name: "sitemap",
            pattern: "sitemap.xml",
            defaults: new { controller = "Sitemap", action = "Index" }
        );

        endpoints.MapControllerRoute(
            name: "areas",
            pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
        );

        endpoints.MapControllerRoute(
            name: "legacyArticleDetails",
            pattern: "news/{categorySlug}/{slug}",
            defaults: new { controller = "Article", action = "LegacyDetails" }
        );

        endpoints.MapControllerRoute(
            name: "legacyCategoryListing",
            pattern: "news/{categorySlug}",
            defaults: new { controller = "Category", action = "LegacyDetails" }
        );

        endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}"
        );

        endpoints.MapControllerRoute(
            name: "articleDetails",
            pattern: "{categorySlug}/{slug}",
            defaults: new { controller = "Article", action = "Details" }
        );

        endpoints.MapControllerRoute(
            name: "categoryListing",
            pattern: "{categorySlug}",
            defaults: new { controller = "Category", action = "Details" }
        );
    }
}
