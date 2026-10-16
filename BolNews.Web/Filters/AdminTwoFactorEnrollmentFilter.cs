using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BolNews.Web.Filters;

public sealed class AdminTwoFactorEnrollmentFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var area = context.RouteData.Values["area"]?.ToString();
        if (string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase) &&
            context.HttpContext.User.IsInRole(Roles.Admin))
        {
            var users = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            var user = await users.GetUserAsync(context.HttpContext.User);
            if (user != null && !await users.GetTwoFactorEnabledAsync(user))
            {
                context.Result = new RedirectToActionResult("EnableAuthenticator", "Account", new { area = "" });
                return;
            }
        }

        await next();
    }
}
