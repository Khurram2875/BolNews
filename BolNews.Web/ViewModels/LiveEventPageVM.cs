namespace BolNews.Web.ViewModels;

public class LiveEventPageVM
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? ImageAlt { get; set; }
    public bool ShowLiveBadge { get; set; }
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

public class LiveEventCarouselItemVM
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? LatestUpdate { get; set; }
    public bool ShowLiveBadge { get; set; }
}
