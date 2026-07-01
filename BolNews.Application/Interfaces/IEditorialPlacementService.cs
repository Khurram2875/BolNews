using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface IEditorialPlacementService
    {
        Task<EditorialPlacement?> GetPinnedTopStoryAsync();
        Task<List<EditorialPlacement>> GetPinnedSecondaryStoriesAsync();
        Task PinTopStoryAsync(int articleId, string currentUserId, IList<string> roles);
        Task UnpinTopStoryAsync(string currentUserId, IList<string> roles);
        Task PinSecondaryStoryAsync(int articleId, string currentUserId, IList<string> roles);
        Task UnpinSecondaryStoryAsync(int placementId, string currentUserId, IList<string> roles);
        Task MoveSecondaryStoryAsync(int placementId, int direction, string currentUserId, IList<string> roles);
    }
}
