using System.ComponentModel.DataAnnotations;
using BolNews.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BolNews.Web.Areas.Admin.ViewModels;

public class LiveEventVM
{
    public int Id { get; set; }
    [Required, StringLength(250)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(191)] public string Slug { get; set; } = string.Empty;
    public int FeaturedMediaId { get; set; }
    public IFormFile? FeaturedImageUpload { get; set; }
    public bool IsActive { get; set; }
    public bool IsPinned { get; set; }
    public bool ShowLiveBadge { get; set; }
    public LiveEventType EventType { get; set; } = LiveEventType.Standard;
    [StringLength(100)] public string? ElectionHeader { get; set; }
    [StringLength(100)] public string? RaceTitle { get; set; }
    [StringLength(100)] public string? RaceSubtitle { get; set; }
    public List<LiveEventCandidateVM> Candidates { get; set; } = new();
    public List<int> ArticleIds { get; set; } = new();
    public List<LiveEventUpdateVM> Updates { get; set; } = new();
    public List<SelectListItem> Images { get; set; } = new();
    public List<LiveEventArticleOptionVM> SelectedArticles { get; set; } = new();
}

public class LiveEventArticleOptionVM
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class LiveEventUpdateVM
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    // Nullable so a blank row can be left empty; the controller validates rows that are partly filled in.
    [StringLength(1000)] public string? Summary { get; set; }
    public string? Details { get; set; }
    [StringLength(500)] public string? Source { get; set; }
}

/// <summary>One candidate row on the election scoreboard. Percentage and votes are typed in by editors.</summary>
public class LiveEventCandidateVM
{
    public int Id { get; set; }
    public ElectionParty Party { get; set; }
    [StringLength(100)] public string? Name { get; set; }
    public decimal Percentage { get; set; }
    public int Votes { get; set; }
}

/// <summary>Lightweight screen for changing only percentages and votes during live coverage.</summary>
public class LiveEventResultsVM
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? RaceTitle { get; set; }
    public string? RaceSubtitle { get; set; }
    public List<LiveEventCandidateVM> Candidates { get; set; } = new();
}
