using BolNews.Domain.Entities;
using BolNews.Web.Areas.Admin.ViewModels;

namespace BolNews.Web.Models;

/// <summary>What each home page block shows, already decided (pinning, counts, de-duplication). Views only render this.</summary>
public sealed class HomeSlotsVM
{
    public PublicArticleVM? TopStory { get; init; }
    public List<PublicArticleVM> TopCards { get; init; } = new();
    public List<PublicArticleVM> Latest { get; init; } = new();
    public List<PublicArticleVM> Featured { get; init; } = new();
    public List<CategorySectionVM> Categories { get; init; } = new();
    public List<string> ShowCarouselImages { get; init; } = new();
    public HomeLayoutSettings Layout { get; init; } = new();
}
