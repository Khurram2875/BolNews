using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Persistence.Repositories;

public class MediaAssetRepository(AppDbContext context) : IMediaAssetRepository
{
    public async Task<int> AddAsync(MediaAsset asset) { context.MediaAssets.Add(asset); await context.SaveChangesAsync(); return asset.Id; }
    public Task<MediaAsset?> GetByIdAsync(int id) => context.MediaAssets.Include(x => x.MediaAssetTags).ThenInclude(x => x.Tag).Include(x => x.FeaturedForArticles).FirstOrDefaultAsync(x => x.Id == id);
    public Task<List<MediaAsset>> SearchAsync(string? query, MediaType? type, DateTime? createdFrom, DateTime? createdTo, int? articleId)
    {
        var assets = context.MediaAssets.AsNoTracking().Include(x => x.MediaAssetTags).ThenInclude(x => x.Tag).Include(x => x.FeaturedForArticles).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query)) assets = assets.Where(x => EF.Functions.Like(x.Caption ?? "", $"%{query}%") || x.MediaAssetTags.Any(t => EF.Functions.Like(t.Tag.Name, $"%{query}%")) || x.FeaturedForArticles.Any(a => EF.Functions.Like(a.Title, $"%{query}%")));
        if (type.HasValue) assets = assets.Where(x => x.MediaType == type);
        if (createdFrom.HasValue) assets = assets.Where(x => x.CreatedAt >= createdFrom.Value);
        if (createdTo.HasValue) assets = assets.Where(x => x.CreatedAt < createdTo.Value.AddDays(1));
        if (articleId.HasValue) assets = assets.Where(x => x.FeaturedForArticles.Any(a => a.Id == articleId));
        return assets.OrderByDescending(x => x.CreatedAt).ToListAsync();
    }
    public async Task UpdateAsync(MediaAsset asset) { context.MediaAssets.Update(asset); await context.SaveChangesAsync(); }
    public async Task AssignAsFeaturedAsync(int articleId, int mediaAssetId, string currentUserId)
    {
        var article = await context.Articles.FindAsync(articleId); var media = await context.MediaAssets.FindAsync(mediaAssetId);
        if (article == null || media == null || media.MediaType != MediaType.Image) throw new InvalidOperationException("Image media was not found.");
        article.FeaturedMediaId = media.Id; article.FeaturedImageXl = media.Url; article.FeaturedImageThumb = media.ThumbnailUrl; article.FeaturedImageMedium = media.MediumUrl; article.FeaturedImageLarge = media.LargeUrl; article.UpdatedAt = DateTime.UtcNow; article.UpdatedBy = currentUserId;
        await context.SaveChangesAsync();
    }
}
