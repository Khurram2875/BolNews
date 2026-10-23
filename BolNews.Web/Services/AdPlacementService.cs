using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Web.Services;

public sealed class AdPlacementService(AppDbContext db, IMemoryCache cache)
{
    private const string CacheKey = "productization:ad-placements:v1";

    public async Task<List<AdPlacement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out List<AdPlacement>? cached) && cached is not null)
            return cached;

        var placements = await db.AdPlacements.AsNoTracking()
            .OrderBy(x => x.ZoneKey)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        cache.Set(CacheKey, placements, TimeSpan.FromMinutes(1));
        return placements;
    }

    public async Task<List<AdPlacement>> GetEnabledForZoneAsync(string zoneKey, CancellationToken cancellationToken = default)
    {
        var placements = await GetAllAsync(cancellationToken);
        return placements.Where(x => x.IsEnabled && x.ZoneKey == zoneKey)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToList();
    }

    public async Task SaveAsync(AdPlacement input, CancellationToken cancellationToken = default)
    {
        if (!AdPlacementZones.Contains(input.ZoneKey))
            throw new InvalidOperationException("Choose one of the supported page zones.");

        input.Key = input.Key.Trim();
        input.DisplayName = input.DisplayName.Trim();
        input.ElementId = input.ElementId.Trim();
        input.ProviderUnit = input.ProviderUnit.Trim();
        input.ContainerClass = input.ContainerClass.Trim();
        if (input.TargetPosition is not ("beforebegin" or "afterbegin" or "beforeend" or "afterend"))
            input.TargetPosition = "beforeend";
        input.InsertionMode = AdPlacementZones.IsInline(input.ZoneKey)
            ? (input.ParagraphIndex.HasValue ? "afterParagraph" : "beforeLastParagraph")
            : "zone";
        input.UpdatedAt = DateTime.UtcNow;

        var existing = input.Id > 0
            ? await db.AdPlacements.FirstOrDefaultAsync(x => x.Id == input.Id, cancellationToken)
            : null;

        if (existing is null)
        {
            input.Id = 0;
            db.AdPlacements.Add(input);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(input);
        }

        await db.SaveChangesAsync(cancellationToken);
        cache.Remove(CacheKey);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var placement = await db.AdPlacements.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (placement is null)
            return;

        db.AdPlacements.Remove(placement);
        await db.SaveChangesAsync(cancellationToken);
        cache.Remove(CacheKey);
    }
}
