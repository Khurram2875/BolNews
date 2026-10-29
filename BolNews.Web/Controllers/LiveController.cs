using BolNews.Domain.Enums;
using BolNews.Persistence.Context;
using BolNews.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Web.Controllers;

public class LiveController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(string slug)
    {
        var item = await db.LiveEvents.AsNoTracking().Where(x => x.IsActive && x.Slug == slug)
            .Select(x => new LiveEventPageVM
            {
                Id = x.Id, Title = x.Title, Slug = x.Slug, CreatedAt = x.CreatedAt,
                ShowLiveBadge = x.ShowLiveBadge,
                IsElection = x.EventType == LiveEventType.Election,
                ElectionHeader = x.ElectionHeader, RaceTitle = x.RaceTitle, RaceSubtitle = x.RaceSubtitle,
                Candidates = x.Candidates.OrderBy(c => c.SortOrder)
                    .Select(c => new LiveEventCandidateCardVM { Party = c.Party, Name = c.Name, Percentage = c.Percentage, Votes = c.Votes }).ToList(),
                ImageUrl = x.FeaturedMedia.LargeUrl ?? x.FeaturedMedia.Url, ImageAlt = x.FeaturedMedia.AltText,
                Updates = x.Updates.OrderByDescending(u => u.Timestamp).Select(u => new LiveEventPageUpdateVM
                { Id = u.Id, Timestamp = u.Timestamp, Summary = u.Summary, Details = u.Details, Source = u.Source, UpdatedBy = u.UpdatedBy }).ToList(),
                Articles = x.LinkedArticles.Where(a => a.Article.IsPublished).Select(a => new LiveEventPageArticleVM
                {
                    Id = a.Article.Id, Title = a.Article.Title, Slug = a.Article.Slug, CategorySlug = a.Article.Category.Slug,
                    ImageUrl = a.Article.FeaturedImageMedium ?? a.Article.FeaturedImageThumb, Summary = a.Article.Summary
                }).ToList()
            }).FirstOrDefaultAsync();
        return item == null ? NotFound() : View(item);
    }
}
