using BolNews.Domain.Entities;
using BolNews.Domain.Enums;
using BolNews.Persistence.Context;
using BolNews.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Web.ViewComponents;

public class LiveEventsViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var events = await db.LiveEvents.AsNoTracking().Where(x => x.IsActive)
            .OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.CreatedAt)
            .Select(x => new LiveEventCarouselItemVM
            {
                Title = x.Title, Slug = x.Slug,
                ImageUrl = x.FeaturedMedia.MediumUrl ?? x.FeaturedMedia.ThumbnailUrl ?? x.FeaturedMedia.Url,
                ShowLiveBadge = x.ShowLiveBadge,
                LatestUpdate = x.Updates.OrderByDescending(u => u.Timestamp).Select(u => u.Summary).FirstOrDefault(),
                IsElection = x.EventType == LiveEventType.Election,
                ElectionHeader = x.ElectionHeader, RaceTitle = x.RaceTitle, RaceSubtitle = x.RaceSubtitle,
                Candidates = x.Candidates.OrderBy(c => c.SortOrder)
                    .Select(c => new LiveEventCandidateCardVM { Party = c.Party, Name = c.Name, Percentage = c.Percentage, Votes = c.Votes }).ToList()
            }).ToListAsync();
        return View(events);
    }
}
