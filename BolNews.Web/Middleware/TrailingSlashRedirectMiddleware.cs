using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace BolNews.Web.Middleware;

public sealed class TrailingSlashRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public TrailingSlashRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var path = request.Path.Value;

        if (request.Method is "GET" or "HEAD"
            && !string.IsNullOrEmpty(path)
            && path.Length > 1
            && path.EndsWith('/')
            && !HasFileExtension(path)
            && !IsExcludedPath(path))
        {
            var canonicalPath = GetCanonicalPath(context, path);
            var location = canonicalPath + request.QueryString;

            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            context.Response.Headers.Location = location;
            return;
        }

        await _next(context);
    }

    private static string GetCanonicalPath(HttpContext context, string path)
    {
        var actionDescriptor = context.GetEndpoint()?
            .Metadata.GetMetadata<ControllerActionDescriptor>();

        if (actionDescriptor?.ControllerName.Equals("Article", StringComparison.OrdinalIgnoreCase) == true
            && actionDescriptor.ActionName.Equals("LegacyDetails", StringComparison.OrdinalIgnoreCase))
        {
            var categorySlug = GetRouteValue(context, "categorySlug");
            var slug = GetRouteValue(context, "slug");

            if (!string.IsNullOrWhiteSpace(categorySlug)
                && !string.IsNullOrWhiteSpace(slug))
            {
                return $"/{categorySlug}/{slug}";
            }
        }

        if (actionDescriptor?.ControllerName.Equals("Category", StringComparison.OrdinalIgnoreCase) == true
            && actionDescriptor.ActionName.Equals("LegacyDetails", StringComparison.OrdinalIgnoreCase))
        {
            var categorySlug = GetRouteValue(context, "categorySlug");

            if (!string.IsNullOrWhiteSpace(categorySlug))
            {
                return $"/category/{categorySlug}";
            }
        }

        return path.TrimEnd('/');
    }

    private static string? GetRouteValue(HttpContext context, string key)
        => context.Request.RouteValues.TryGetValue(key, out var value)
            ? Convert.ToString(value)
            : null;

    private static bool HasFileExtension(string path)
    {
        var lastSegment = path[(path.LastIndexOf('/') + 1)..];
        return Path.HasExtension(lastSegment);
    }

    private static bool IsExcludedPath(string path)
    {
        return path.Equals("/Admin", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Admin/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/notificationHub", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/notificationHub/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/health/", StringComparison.OrdinalIgnoreCase);
    }
}
