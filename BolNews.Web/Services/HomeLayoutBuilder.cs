using BolNews.Domain.Entities;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Models;

namespace BolNews.Web.Services;

/// <summary>
/// The single place that decides which story goes into which home page slot. It reads the lists the controller
/// prepared (pinned items first, then organic) and applies the publisher's counts. With default settings the result
/// is identical to the previous hard-coded view logic: top story + 2 cards, 14 latest, 10 featured.
/// Pinned stories always win over counts, and a story is never shown twice.
/// </summary>
public static class HomeLayoutBuilder
{
    public static HomeSlotsVM Build(HomePageVM model, HomeLayoutSettings settings)
    {
        var s = HomeLayoutSettings.Normalize(settings);

        var secondary = model.SecondaryStories ?? new List<PublicArticleVM>();
        var pinnedCount = Math.Min(model.PinnedSecondaryStoryCount, secondary.Count);
        var pinnedSecondary = secondary.Take(pinnedCount).ToList();
        var unpinnedSecondary = secondary.Skip(pinnedCount).ToList();

        // Top stories panel: pinned "secondary" stories first, then organic ones, up to TopCardCount.
        var k = s.TopCardCount;
        var topSlotCount = Math.Min(k, pinnedSecondary.Count);
        var topCards = pinnedSecondary.Take(k)
            .Concat(unpinnedSecondary.Take(k - topSlotCount))
            .ToList();

        // Latest rail: pinned latest first, then whatever the top panel did not use.
        var pinnedLatest = model.PinnedLatestStories ?? new List<PublicArticleVM>();
        var latestRemaining = Math.Max(0, s.LatestCount - pinnedLatest.Count);
        var organicLatestPool = pinnedSecondary.Skip(k).Concat(unpinnedSecondary.Skip(k - topSlotCount));
        var latest = s.LatestEnabled
            ? pinnedLatest.Concat(organicLatestPool.Take(latestRemaining)).ToList()
            : new List<PublicArticleVM>();

        // Featured rail: pinned featured first, then organic stories not already shown elsewhere.
        var pinnedFeatured = model.PinnedFeaturedStories ?? new List<PublicArticleVM>();
        var used = new HashSet<int>();
        if (model.TopStory != null) used.Add(model.TopStory.Id);
        foreach (var a in topCards) used.Add(a.Id);
        foreach (var a in latest) used.Add(a.Id);
        foreach (var a in pinnedFeatured) used.Add(a.Id);

        var featured = new List<PublicArticleVM>();
        if (s.FeaturedEnabled)
        {
            var featuredRemaining = Math.Max(0, s.FeaturedCount - pinnedFeatured.Count);
            var organicFeatured = (model.FeaturedStories ?? new List<PublicArticleVM>())
                .Where(a => !used.Contains(a.Id))
                .Take(featuredRemaining);
            featured = pinnedFeatured.Concat(organicFeatured).ToList();
        }

        return new HomeSlotsVM
        {
            TopStory = model.TopStory,
            TopCards = topCards,
            Latest = latest,
            Featured = featured,
            Categories = BuildCategories(model.CategorySections ?? new List<CategorySectionVM>(), s),
            ShowCarouselImages = model.ShowCarouselImages ?? new List<string>(),
            Layout = s
        };
    }

    private static List<CategorySectionVM> BuildCategories(List<CategorySectionVM> sections, HomeLayoutSettings s)
    {
        if (!s.CategoriesEnabled) return new();

        CategorySectionVM? Find(string slug) =>
            sections.FirstOrDefault(c => string.Equals(c.CategorySlug, slug, StringComparison.OrdinalIgnoreCase));

        var rows = s.CategoriesCustomized
            ? s.Categories.Where(r => r.Enabled).ToList()
            : HomeLayoutSettings.DefaultCategorySlugs.Select((slug, i) => new HomeCategoryRow { Slug = slug, Count = 4, Order = i + 1 }).ToList();

        var result = new List<CategorySectionVM>();
        foreach (var row in rows)
        {
            var section = Find(row.Slug);
            if (section == null || section.Articles.Count == 0) continue;
            // Copy so the cached home page model is never modified.
            result.Add(new CategorySectionVM
            {
                CategoryName = section.CategoryName,
                CategorySlug = section.CategorySlug,
                MetaTitle = section.MetaTitle,
                MetaDescription = section.MetaDescription,
                Articles = section.Articles.Take(row.Count).ToList()
            });
        }
        return result;
    }
}
