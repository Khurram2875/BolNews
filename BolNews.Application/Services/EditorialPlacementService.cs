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

        public Task<List<EditorialPlacement>> GetPinnedLatestStoriesAsync()
            => _placementRepository.GetActivePlacementsWithArticlesAsync(
                EditorialPlacementKeys.HomepageLatestStory,
                EditorialPlacementKeys.HomepageLatestStoryLimit);

        public Task<List<EditorialPlacement>> GetPinnedFeaturedStoriesAsync()
            => _placementRepository.GetActivePlacementsWithArticlesAsync(
                EditorialPlacementKeys.HomepageFeaturedStory,
                EditorialPlacementKeys.HomepageFeaturedStoryLimit);


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
                await NormalizeOrderAsync(EditorialPlacementKeys.HomepageSecondaryStory);
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

        public Task PinSecondaryStoryAsync(int articleId, string currentUserId, IList<string> roles)
        => PinToListAsync(EditorialPlacementKeys.HomepageSecondaryStory, articleId, currentUserId, roles);

        public Task UnpinSecondaryStoryAsync(int placementId, string currentUserId, IList<string> roles)
            => UnpinFromListAsync(EditorialPlacementKeys.HomepageSecondaryStory, placementId, currentUserId, roles);

        public Task MoveSecondaryStoryAsync(int placementId, int direction, string currentUserId, IList<string> roles)
            => MoveWithinListAsync(EditorialPlacementKeys.HomepageSecondaryStory, placementId, direction, currentUserId, roles);

        public Task PinLatestStoryAsync(int articleId, string currentUserId, IList<string> roles)
            => PinToListAsync(EditorialPlacementKeys.HomepageLatestStory, articleId, currentUserId, roles);

        public Task UnpinLatestStoryAsync(int placementId, string currentUserId, IList<string> roles)
            => UnpinFromListAsync(EditorialPlacementKeys.HomepageLatestStory, placementId, currentUserId, roles);

        public Task MoveLatestStoryAsync(int placementId, int direction, string currentUserId, IList<string> roles)
            => MoveWithinListAsync(EditorialPlacementKeys.HomepageLatestStory, placementId, direction, currentUserId, roles);

        public Task PinFeaturedStoryAsync(int articleId, string currentUserId, IList<string> roles)
            => PinToListAsync(EditorialPlacementKeys.HomepageFeaturedStory, articleId, currentUserId, roles);

        public Task UnpinFeaturedStoryAsync(int placementId, string currentUserId, IList<string> roles)
            => UnpinFromListAsync(EditorialPlacementKeys.HomepageFeaturedStory, placementId, currentUserId, roles);

        public Task MoveFeaturedStoryAsync(int placementId, int direction, string currentUserId, IList<string> roles)
            => MoveWithinListAsync(EditorialPlacementKeys.HomepageFeaturedStory, placementId, direction, currentUserId, roles);

        // ── Shared implementation, parameterized by placement key ──────────────

        private async Task PinToListAsync(string placementKey, int articleId, string currentUserId, IList<string> roles)
        {
            EnsureCanManagePlacements(roles);
            await EnsurePublishedArticleExists(articleId);

            var topStory = await _placementRepository.GetActivePlacementAsync(EditorialPlacementKeys.HomepageTopStory);
            if (topStory?.ArticleId == articleId)
            {
                throw new InvalidOperationException("This article is already pinned as the homepage top story.");
            }

            var existing = await _placementRepository.GetActivePlacementByArticleAsync(placementKey, articleId);
            if (existing != null)
            {
                return;
            }

            await EnsurePlacementLimitAsync(placementKey);

            var sortOrder = await GetNewPlacementSortOrderAsync(placementKey);

            await _placementRepository.AddAsync(new EditorialPlacement
            {
                PlacementKey = placementKey,
                ArticleId = articleId,
                SortOrder = sortOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId
            });

            await _placementRepository.SaveChangesAsync();
            InvalidateHomepageCache();
        }

        private async Task UnpinFromListAsync(string placementKey, int placementId, string currentUserId, IList<string> roles)
        {
            EnsureCanManagePlacements(roles);

            var placement = await GetPlacementOrThrow(placementKey, placementId);

            await _placementRepository.SoftDeleteAsync(placement, currentUserId);
            await NormalizeOrderAsync(placementKey);
            await _placementRepository.SaveChangesAsync();
            InvalidateHomepageCache();
        }

        private async Task MoveWithinListAsync(string placementKey, int placementId, int direction, string currentUserId, IList<string> roles)
        {
            EnsureCanManagePlacements(roles);

            if (direction != -1 && direction != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(direction), "Direction must be -1 or 1.");
            }

            var placements = await _placementRepository.GetActivePlacementsAsync(placementKey);
            var currentIndex = placements.FindIndex(x => x.Id == placementId);

            if (currentIndex < 0)
            {
                throw new InvalidOperationException("Placement was not found.");
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

        private async Task<EditorialPlacement> GetPlacementOrThrow(string placementKey, int placementId)
        {
            var placement = await _placementRepository.GetActivePlacementByIdAsync(placementId);

            if (placement == null || placement.PlacementKey != placementKey || placement.IsDeleted)
            {
                throw new InvalidOperationException("Placement was not found.");
            }

            return placement;
        }

        private async Task NormalizeOrderAsync(string placementKey)
        {
            var placements = await _placementRepository.GetActivePlacementsAsync(placementKey);

            for (var i = 0; i < placements.Count; i++)
            {
                placements[i].SortOrder = i + 1;
            }
        }

        private async Task<int> GetNewPlacementSortOrderAsync(string placementKey)
        {
            if (placementKey is EditorialPlacementKeys.HomepageLatestStory or EditorialPlacementKeys.HomepageFeaturedStory)
            {
                var placements = await _placementRepository.GetActivePlacementsAsync(placementKey);

                foreach (var placement in placements)
                {
                    placement.SortOrder++;
                }

                return 1;
            }

            return await _placementRepository.GetMaxSortOrderAsync(placementKey) + 1;
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
            if (!roles.Contains(Roles.Admin) && !roles.Contains(Roles.Editor) && !roles.Contains(Roles.SubEditor))
            {
                throw new UnauthorizedAccessException("Only Editors and Admins can manage homepage story placement.");
            }
        }

        private async Task EnsurePlacementLimitAsync(string placementKey)
        {
            var limit = placementKey switch
            {
                EditorialPlacementKeys.HomepageLatestStory => EditorialPlacementKeys.HomepageLatestStoryLimit,
                EditorialPlacementKeys.HomepageFeaturedStory => EditorialPlacementKeys.HomepageFeaturedStoryLimit,
                _ => 0
            };

            if (limit <= 0)
            {
                return;
            }

            var currentCount = await _placementRepository.CountActivePlacementsAsync(placementKey);
            if (currentCount >= limit)
            {
                var placementName = placementKey == EditorialPlacementKeys.HomepageLatestStory
                    ? "Latest stories"
                    : "Featured stories";

                throw new InvalidOperationException($"{placementName} pinning is limited to {limit} articles.");
            }
        }

        private void InvalidateHomepageCache()
        {
            _cacheService.InvalidateHomePage();
        }
    }
}
