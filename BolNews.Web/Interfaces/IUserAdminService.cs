using BolNews.Domain.Entities;
using BolNews.Web.Models;

namespace BolNews.Web.Interfaces
{
    public interface IUserAdminService
    {
        Task<List<UserWithRolesVM>> GetUsersWithRolesAsync(bool? isActive = true, string? currentUserId = null);
        Task<(ApplicationUser? user, List<string> roles, IList<string> userRoles)> GetAssignRoleDataAsync(string id);
        Task UpdateUserRolesAsync(string userId,List<string> selectedRoles);
        Task<bool> SetUserActiveAsync(string userId, bool isActive);
    }
}
