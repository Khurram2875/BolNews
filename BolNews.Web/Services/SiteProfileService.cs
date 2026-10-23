using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Web.Services;

public sealed class SiteProfileService(AppDbContext db, IMemoryCache cache)
{
    private const string CacheKey = "productization:site-profile:v1";

    public async Task<SiteProfile> GetAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out SiteProfile? cached) && cached is not null)
            return cached;

        var profile = await db.SiteProfiles.AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? new SiteProfile();

        cache.Set(CacheKey, profile, TimeSpan.FromMinutes(2));
        return profile;
    }

    public async Task SaveAsync(SiteProfile input, CancellationToken cancellationToken = default)
    {
        var profile = await db.SiteProfiles.FirstOrDefaultAsync(cancellationToken);
        if (profile is null)
        {
            input.Id = 1;
            input.UpdatedAt = DateTime.UtcNow;
            db.SiteProfiles.Add(input);
        }
        else
        {
            db.Entry(profile).CurrentValues.SetValues(input);
            profile.Id = 1;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        cache.Remove(CacheKey);
    }
}
