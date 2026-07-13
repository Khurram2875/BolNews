using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface IBreakingNewsService
    {
        Task<List<BreakingNews>> GetAllAsync();

        Task<List<BreakingNews>> GetActiveAsync();

        Task<List<BreakingNews>> GetInactiveAsync();

        Task<BreakingNews?> GetByIdAsync(int id);

        Task CreateAsync(BreakingNews news, string currentUserId);

        Task UpdateAsync(BreakingNews news, string currentUserId);

        Task DeleteAsync(int id, string currentUserId);

        Task ActivateAsync(int id, string currentUserId);

        Task DeactivateAsync(int id, string currentUserId);
    }
}