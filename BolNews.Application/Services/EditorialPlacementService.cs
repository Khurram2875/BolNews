using BolNews.Application.Common;
using BolNews.Application.Interfaces;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services
{
    public class EditorialPlacementService : IEditorialPlacementService
    {
        private readonly IEditorialPlacementRepository _placementRepository;
        private readonly IArticleRepository _articleRepository;
        private readonly ICacheService _cacheService;

        public EditorialPlacementService(
            IEditorialPlacementRepository placementRepository,
            IArticleRepository articleRepository,
            ICacheService cacheService)
        {
            _placementRepository = placementRepository;
            _articleRepository = articleRepository;
            _cacheService = cacheService;
        }

        public Task<EditorialPlacement?> GetPinnedTopStoryAsync()
            => _placementRepository.GetActivePlacementAsync(EditorialPlacementKeys.HomepageTopStory);

        public Task<List<EditorialPlacement>> GetPinnedSecondaryStoriesAsync()
            => _placementRepository.GetActivePlacementsWithArticlesAsync(EditorialPlacementKeys.HomepageSecondaryStory);

        public async Task PinTopStoryAsync(int articleId, string currentUserId, IList<string> roles)
        {
            EnsureCanManagePlacements(roles);
            await EnsurePublishedArticleExists(articleId);

            var currentTopStory = await _placementRepository.GetActivePlacementAsync(EditorialPlacementKeys.HomepageTopStory);
            if (currentTopStory?.ArticleId == articleId)
            {
                return;
            }

            await _placementRepository.SoftDeleteByPlacementAsync(EditorialPlacementKeys.HomepageTopStory, currentUserId);

            var matchingSecondaryStory = await _placementRepository.GetActivePlacementByArticleAsync(
                EditorialPlacementKeys.HomepageSecondaryStory,
                articleId);

            if (matchingSecondaryStory != null)
            {
                await _placementRepository.SoftDeleteAsync(matchingSecondaryStory, currentUserId);
                await NormalizeSecondaryOrderAsync();
            }

            await _placementRepository.AddAsync(new EditorialPlacement
            {
                PlacementKey = EditorialPlacementKeys.HomepageTopStory,
                ArticleId = articleId,
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId
            });

            await _placementRepository.SaveChangesAsync();
            InvalidateHomepageCache();
        }

        public async Task UnpinTopStoryAsync(string currentUserId, IList<string> roles)
        {
            EnsureCanManagePlacements(roles);

            await _placementRepository.SoftDeleteByPlacementAsync(EditorialPlacementKeys.HomepageTopStory, currentUserId);
            await _placementRepository.SaveChangesAsync();
            InvalidateHomepageCache();
        }

        public async Task PinSecondaryStoryAsync(int articleId, string currentUserId, IList<string> roles)
        {
            EnsureCanManagePlacements(roles);
            await EnsurePublishedArticleExists(articleId);

            var topStory = await _placementRepository.GetActivePlacementAsync(EditorialPlacementKeys.HomepageTopStory);
            if (topStory?.ArticleId == articleId)
            {
                throw new InvalidOperationException("This article is already pinned as the homepage top story.");
            }

            var existingSecondary = await _placementRepository.GetActivePlacementByArticleAsync(
                EditorialPlacementKeys.HomepageSecondaryStory,
                articleId);

            if (existingSecondary != null)
            {
                return;
            }

            var sortOrder = await _placementRepository.GetMaxSortOrderAsync(EditorialPlacementKeys.HomepageSecondaryStory) + 1;

            await _placementRepository.AddAsync(new EditorialPlacement
            {
                PlacementKey = EditorialPlacementKeys.HomepageSecondaryStory,
                ArticleId = articleId,
                SortOrder = sortOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId
            });

            await _placementRepository.SaveChangesAsync();
            InvalidateHomepageCache();
        }

        public async Task UnpinSecondaryStoryAsync(int placementId, string currentUserId, IList<string> roles)
        {
            EnsureCanManagePlacements(roles);

            var placement = await GetSecondaryPlacementOrThrow(placementId);

            await _placementRepository.SoftDeleteAsync(placement, currentUserId);
            await NormalizeSecondaryOrderAsync();
            await _placementRepository.SaveChangesAsync();
            InvalidateHomepageCache();
        }

        public async Task MoveSecondaryStoryAsync(int placementId, int direction, string currentUserId, IList<string> roles)
        {
            EnsureCanManagePlacements(roles);

            if (direction != -1 && direction != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(direction), "Direction must be -1 or 1.");
            }

            var placements = await _placementRepository.GetActivePlacementsAsync(EditorialPlacementKeys.HomepageSecondaryStory);
            var currentIndex = placements.FindIndex(x => x.Id == placementId);

            if (currentIndex < 0)
            {
                throw new InvalidOperationException("Secondary story placement was not found.");
            }

            var targetIndex = currentIndex + direction;
            if (targetIndex < 0 || targetIndex >= placements.Count)
            {
                return;
            }

            (placements[currentIndex].SortOrder, placements[targetIndex].SortOrder) =
                (placements[targetIndex].SortOrder, placements[currentIndex].SortOrder);

            placements[currentIndex].UpdatedAt = DateTime.UtcNow;
            placements[currentIndex].UpdatedBy = currentUserId;
            placements[targetIndex].UpdatedAt = DateTime.UtcNow;
            placements[targetIndex].UpdatedBy = currentUserId;

            await _placementRepository.SaveChangesAsync();
            InvalidateHomepageCache();
        }

        private async Task<EditorialPlacement> GetSecondaryPlacementOrThrow(int placementId)
        {
            var placement = await _placementRepository.GetActivePlacementByIdAsync(placementId);

            if (placement == null ||
                placement.PlacementKey != EditorialPlacementKeys.HomepageSecondaryStory ||
                placement.IsDeleted)
            {
                throw new InvalidOperationException("Secondary story placement was not found.");
            }

            return placement;
        }

        private async Task NormalizeSecondaryOrderAsync()
        {
            var placements = await _placementRepository.GetActivePlacementsAsync(EditorialPlacementKeys.HomepageSecondaryStory);

            for (var i = 0; i < placements.Count; i++)
            {
                placements[i].SortOrder = i + 1;
            }
        }

        private async Task EnsurePublishedArticleExists(int articleId)
        {
            var article = await _articleRepository.FindPublishedByIdAsync(articleId);

            if (article == null)
            {
                throw new InvalidOperationException("Only published articles can be pinned to the homepage.");
            }
        }

        private static void EnsureCanManagePlacements(IList<string> roles)
        {
            if (!roles.Contains(Roles.Admin) && !roles.Contains(Roles.Editor))
            {
                throw new UnauthorizedAccessException("Only Editors and Admins can manage homepage story placement.");
            }
        }

        private void InvalidateHomepageCache()
        {
            _cacheService.Remove(CacheKeys.HomePage);
        }
    }
}
