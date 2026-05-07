using BolNews.Domain.Entities;
using BolNews.Web.Models;

namespace BolNews.Web.Interfaces
{
    public interface IUserAdminService
    {
        Task<List<UserWithRolesVM>> GetUsersWithRolesAsync();
        Task<(ApplicationUser? user, List<string> roles, IList<string> userRoles)> GetAssignRoleDataAsync(string id);
        Task AssignSingleRoleAsync(string userId, string role);
    }
}
