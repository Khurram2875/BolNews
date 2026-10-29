using BolNews.Domain.Enums;

namespace BolNews.Web.ViewModels;

/// <summary>
/// Everything the US election scoreboard card needs. Shared by the homepage carousel item
/// and the live page so both render the same card partial.
/// </summary>
public class LiveEventElectionCardVM
{
    public bool IsElection { get; set; }
    public string? ElectionHeader { get; set; }
    public string? RaceTitle { get; set; }
    public string? RaceSubtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool ShowLiveBadge { get; set; }
    public List<LiveEventCandidateCardVM> Candidates { get; set; } = new();
}

public class LiveEventCandidateCardVM
{
    public ElectionParty Party { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public int Votes { get; set; }
}

public class LiveEventPageVM : LiveEventElectionCardVM
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageAlt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<LiveEventPageUpdateVM> Updates { get; set; } = new();
    public List<LiveEventPageArticleVM> Articles { get; set; } = new();
}

public class LiveEventPageUpdateVM
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class LiveEventPageArticleVM
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Summary { get; set; }
}

public class LiveEventCarouselItemVM : LiveEventElectionCardVM
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LatestUpdate { get; set; }
}
