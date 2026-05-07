using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using BolNews.Web.Interfaces;
using BolNews.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Web.Services
{
    public class UserAdminService : IUserAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public UserAdminService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<List<UserWithRolesVM>> GetUsersWithRolesAsync()
        {
            // Eliminates N+1 role lookups by loading users + role mappings in set-based queries.
            var users = await _userManager.Users
                .AsNoTracking()
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToListAsync();

            var roleRows = await (
                from ur in _context.UserRoles.AsNoTracking()
                join r in _context.Roles.AsNoTracking() on ur.RoleId equals r.Id
                select new { ur.UserId, RoleName = r.Name }
            ).ToListAsync();

            var roleLookup = roleRows
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Where(x => !string.IsNullOrWhiteSpace(x.RoleName))
                                                 .Select(x => x.RoleName!)
                                                 .ToList());

            return users
                .Select(u => new UserWithRolesVM
                {
                    Id = u.Id,
                    FullName = u.FullName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Roles = roleLookup.TryGetValue(u.Id, out var roles) ? roles : new List<string>()
                })
                .ToList();
        }

        public async Task<(ApplicationUser? user, List<string> roles, IList<string> userRoles)> GetAssignRoleDataAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            var roles = await _roleManager.Roles
                .AsNoTracking()
                .Select(r => r.Name!)
                .ToListAsync();

            if (user == null)
                return (null, roles, new List<string>());

            var userRoles = await (
                from ur in _context.UserRoles.AsNoTracking()
                join r in _context.Roles.AsNoTracking() on ur.RoleId equals r.Id
                where ur.UserId == user.Id && r.Name != null
                select r.Name!
            ).ToListAsync();

            return (user, roles, userRoles);
        }

        public async Task AssignSingleRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);
        }
    }
}
