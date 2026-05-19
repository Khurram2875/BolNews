using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using BolNews.Domain.Common;

namespace BolNews.Persistence.Identity
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles =
            {
                Roles.Admin,
                Roles.Editor,
                Roles.SubEditor,
                Roles.Author,
                Roles.User
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            var adminEmail = "admin@cms.com";

            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Admin"
                };

                var adminPassword = Environment.GetEnvironmentVariable("BOLNEWS_ADMIN_PASSWORD");
                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    throw new InvalidOperationException(
                        "Environment variable 'BOLNEWS_ADMIN_PASSWORD' must be set for initial admin seeding.");
                }

                await userManager.CreateAsync(admin, adminPassword);
                await userManager.AddToRoleAsync(admin, Roles.Admin);
            }
        }
    }
}
