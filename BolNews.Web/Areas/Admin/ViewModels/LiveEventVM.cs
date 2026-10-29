using System.ComponentModel.DataAnnotations;
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
    [Required, StringLength(1000)] public string Summary { get; set; } = string.Empty;
    [Required] public string Details { get; set; } = string.Empty;
    [StringLength(500)] public string? Source { get; set; }
}
