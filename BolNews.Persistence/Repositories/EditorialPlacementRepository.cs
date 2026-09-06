using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Persistence.Repositories
{
    public class EditorialPlacementRepository : IEditorialPlacementRepository
    {
        private readonly AppDbContext _context;

        public EditorialPlacementRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<EditorialPlacement?> GetActivePlacementAsync(string placementKey)
            => await ActivePlacements(placementKey)
                .Include(x => x.Article)
                    .ThenInclude(x => x.Category)
                .OrderBy(x => x.SortOrder)
                .FirstOrDefaultAsync();

        public async Task<EditorialPlacement?> GetActivePlacementByArticleAsync(string placementKey, int articleId)
            => await ActivePlacements(placementKey)
                .FirstOrDefaultAsync(x => x.ArticleId == articleId);

        public async Task<EditorialPlacement?> GetActivePlacementByIdAsync(int placementId)
            => await _context.EditorialPlacements
                .Include(x => x.Article)
                .FirstOrDefaultAsync(x => x.Id == placementId);

        public async Task<List<EditorialPlacement>> GetActivePlacementsAsync(string placementKey)
            => await ActivePlacements(placementKey)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

        public async Task<List<EditorialPlacement>> GetActivePlacementsWithArticlesAsync(string placementKey)
            => await ActivePlacements(placementKey)
                .Include(x => x.Article)
                    .ThenInclude(x => x.Category)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

        public async Task<List<EditorialPlacement>> GetActivePlacementsWithArticlesAsync(string placementKey, int take)
        {
            IQueryable<EditorialPlacement> query = ActivePlacements(placementKey)
                .AsNoTracking()
                .Include(x => x.Article)
                    .ThenInclude(x => x.Category)
                .Where(x => x.Article.IsPublished && !x.Article.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id);

            if (take > 0)
            {
                query = query.Take(take);
            }

            return await query.ToListAsync();
        }

        public async Task<int> CountActivePlacementsAsync(string placementKey)
            => await ActivePlacements(placementKey).CountAsync();

        public Task AddAsync(EditorialPlacement placement)
        {
            _context.EditorialPlacements.Add(placement);
            return Task.CompletedTask;
        }

        public Task SoftDeleteAsync(EditorialPlacement placement, string currentUserId)
        {
            placement.IsDeleted = true;
            placement.UpdatedAt = DateTime.UtcNow;
            placement.UpdatedBy = currentUserId;
            return Task.CompletedTask;
        }

        public async Task SoftDeleteByPlacementAsync(string placementKey, string currentUserId)
        {
            var placements = await ActivePlacements(placementKey).ToListAsync();

            foreach (var placement in placements)
            {
                placement.IsDeleted = true;
                placement.UpdatedAt = DateTime.UtcNow;
                placement.UpdatedBy = currentUserId;
            }
        }

        public async Task<int> GetMaxSortOrderAsync(string placementKey)
        {
            var sortOrders = ActivePlacements(placementKey)
                .Select(x => (int?)x.SortOrder);

            return await sortOrders.MaxAsync() ?? 0;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        private IQueryable<EditorialPlacement> ActivePlacements(string placementKey)
            => _context.EditorialPlacements
                .Where(x => x.PlacementKey == placementKey && !x.IsDeleted);
    }
}
