using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface IEditorialPlacementRepository
    {
        Task<EditorialPlacement?> GetActivePlacementAsync(string placementKey);
        Task<EditorialPlacement?> GetActivePlacementByArticleAsync(string placementKey, int articleId);
        Task<EditorialPlacement?> GetActivePlacementByIdAsync(int placementId);
        Task<List<EditorialPlacement>> GetActivePlacementsAsync(string placementKey);
        Task<List<EditorialPlacement>> GetActivePlacementsWithArticlesAsync(string placementKey);
        Task<List<EditorialPlacement>> GetActivePlacementsWithArticlesAsync(string placementKey, int take);
        Task<int> CountActivePlacementsAsync(string placementKey);
        Task AddAsync(EditorialPlacement placement);
        Task SoftDeleteAsync(EditorialPlacement placement, string currentUserId);
        Task SoftDeleteByPlacementAsync(string placementKey, string currentUserId);
        Task<int> GetMaxSortOrderAsync(string placementKey);
        Task SaveChangesAsync();
    }
}
