using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace BolNews.Web.Services;

public sealed class SiteProfileService(AppDbContext db, IMemoryCache cache, IHttpContextAccessor http)
{
    public const string PreviewCookie = "bn_preview";
    private const string PreviewKeyPrefix = "productization:site-profile:preview:";
    private static readonly TimeSpan PreviewLifetime = TimeSpan.FromMinutes(30);
    private const string CacheKey = "productization:site-profile:v1";

    /// <summary>The unsaved draft being previewed by the current admin request, or null.</summary>
    public SiteProfile? GetPreviewDraft()
    {
        var ctx = http.HttpContext;
        if (ctx is null || !ctx.Request.Cookies.TryGetValue(PreviewCookie, out var token)) return null;
        if (token.Length != 32 || !token.All(Uri.IsHexDigit)) return null;
        // Preview only affects the public site, never the admin area, and only for signed-in admins.
        if (ctx.Request.Path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase)) return null;
        if (ctx.User.Identity?.IsAuthenticated != true || !ctx.User.IsInRole(BolNews.Domain.Common.Roles.Admin)) return null;
        return cache.TryGetValue(PreviewKeyPrefix + token, out SiteProfile? draft) ? draft : null;
    }

    /// <summary>Keeps an unsaved profile in memory for a short time and returns the token that selects it.</summary>
    public string StorePreview(SiteProfile draft)
    {
        draft.StyleSettingsJson = StyleSettings.Normalize(draft.Style).ToJson();
        draft.CustomBlocksJson = draft.Blocks.ToJson();
        draft.HomeLayoutJson = draft.HomeLayout.ToJson();
        var token = Guid.NewGuid().ToString("N");
        cache.Set(PreviewKeyPrefix + token, draft, PreviewLifetime);
        return token;
    }

    public async Task<SiteProfile> GetAsync(CancellationToken cancellationToken = default)
    {
        var draft = GetPreviewDraft();
        if (draft is not null) return draft;

        if (cache.TryGetValue(CacheKey, out SiteProfile? cached) && cached is not null)
            return cached;

        var profile = await db.SiteProfiles.AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? new SiteProfile();

        cache.Set(CacheKey, profile, TimeSpan.FromMinutes(2));
        return profile;
    }

    public const int MaxVersions = 20;

    public async Task SaveAsync(SiteProfile input, CancellationToken cancellationToken = default, string? user = null, string note = "Saved")
    {
        input.StyleSettingsJson = StyleSettings.Normalize(input.Style).ToJson();
        input.CustomBlocksJson = input.Blocks.ToJson();
        input.HomeLayoutJson = input.HomeLayout.ToJson();
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
        await AddVersionAsync(profile ?? input, user, note, cancellationToken);
        cache.Remove(CacheKey);
    }

    private async Task AddVersionAsync(SiteProfile p, string? user, string note, CancellationToken ct)
    {
        var snap = new SiteProfileStyleSnapshot
        {
            CustomCss = p.CustomCss, StyleSettingsJson = p.StyleSettingsJson, CustomBlocksJson = p.CustomBlocksJson, HomeLayoutJson = p.HomeLayoutJson, ThemeName = p.ThemeName,
            PrimaryColor = p.PrimaryColor, AccentColor = p.AccentColor, FontFamily = p.FontFamily,
            BaseFontSizePx = p.BaseFontSizePx, ArticleFontSizePx = p.ArticleFontSizePx
        };
        var json = JsonSerializer.Serialize(snap);
        var latest = await db.SiteProfileVersions.OrderByDescending(x => x.Id).FirstOrDefaultAsync(ct);
        if (latest is not null && latest.SnapshotJson == json) return; // nothing style-related changed
        db.SiteProfileVersions.Add(new SiteProfileVersion
        {
            CreatedAt = DateTime.UtcNow, CreatedBy = user, Note = note.Length > 200 ? note[..200] : note, SnapshotJson = json
        });
        await db.SaveChangesAsync(ct);
        var stale = await db.SiteProfileVersions.OrderByDescending(x => x.Id).Skip(MaxVersions).ToListAsync(ct);
        if (stale.Count > 0)
        {
            db.SiteProfileVersions.RemoveRange(stale);
            await db.SaveChangesAsync(ct);
        }
    }

    public Task<List<SiteProfileVersion>> GetVersionsAsync(CancellationToken ct = default) =>
        db.SiteProfileVersions.AsNoTracking().OrderByDescending(x => x.Id).Take(MaxVersions).ToListAsync(ct);

    public async Task<SiteProfileVersion?> GetVersionAsync(int id, CancellationToken ct = default) =>
        await db.SiteProfileVersions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    /// <summary>Copies a version's style fields onto the live profile (other settings are untouched) and records it as a new version.</summary>
    public async Task<bool> RestoreAsync(int versionId, string? user, CancellationToken ct = default)
    {
        var version = await GetVersionAsync(versionId, ct);
        if (version is null) return false;
        SiteProfileStyleSnapshot? snap;
        try { snap = JsonSerializer.Deserialize<SiteProfileStyleSnapshot>(version.SnapshotJson); }
        catch (JsonException) { return false; }
        if (snap is null) return false;
        var profile = await db.SiteProfiles.FirstOrDefaultAsync(ct);
        if (profile is null) return false;
        profile.CustomCss = snap.CustomCss; profile.StyleSettingsJson = snap.StyleSettingsJson;
        if (snap.CustomBlocksJson is not null) profile.CustomBlocksJson = snap.CustomBlocksJson;
        if (snap.HomeLayoutJson is not null) profile.HomeLayoutJson = snap.HomeLayoutJson;
        profile.ThemeName = snap.ThemeName;
        profile.PrimaryColor = snap.PrimaryColor; profile.AccentColor = snap.AccentColor; profile.FontFamily = snap.FontFamily;
        profile.BaseFontSizePx = snap.BaseFontSizePx; profile.ArticleFontSizePx = snap.ArticleFontSizePx;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await AddVersionAsync(profile, user, $"Restored version #{versionId}", ct);
        cache.Remove(CacheKey);
        return true;
    }

    /// <summary>Clears the style controls (typography/colour/roundness) and the custom CSS; brand colours, theme and fonts are kept.</summary>
    public async Task ResetStyleAsync(string? user, CancellationToken ct = default)
    {
        var profile = await db.SiteProfiles.FirstOrDefaultAsync(ct);
        if (profile is null) return;
        profile.StyleSettingsJson = null; profile.CustomCss = null;
        profile.BaseFontSizePx = 16; profile.ArticleFontSizePx = 0;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await AddVersionAsync(profile, user, "Reset style to default", ct);
        cache.Remove(CacheKey);
    }
}
