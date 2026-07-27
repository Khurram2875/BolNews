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
            name: "aboutUs",
            pattern: "about-us",
            defaults: new { controller = "Home", action = "About" }
        );

        endpoints.MapControllerRoute("contactUs", "contact-us", new { controller = "Home", action = "ContactUs" });
        endpoints.MapControllerRoute("advertise", "advertise", new { controller = "Home", action = "Advertise" });
        endpoints.MapControllerRoute("blogs", "blogs", new { controller = "Home", action = "Blogs" });
        endpoints.MapControllerRoute("brandedContent", "branded-content", new { controller = "Home", action = "BrandedContent" });
        endpoints.MapControllerRoute("editorialPolicy", "editorial-policy", new { controller = "Home", action = "EditorialPolicy" });
        endpoints.MapControllerRoute("privacyPolicy", "privacy-policy", new { controller = "Home", action = "Privacy" });
        endpoints.MapControllerRoute("termsOfService", "terms-of-service", new { controller = "Home", action = "TermsOfService" });

        endpoints.MapControllerRoute(
            name: "categoryListing",
            pattern: "category/{categorySlug}",
            defaults: new { controller = "Category", action = "Details" }
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
            name: "legacyShortCategoryListing",
            pattern: "{categorySlug}",
            defaults: new { controller = "Category", action = "LegacyDetails" }
        );
    }
}
