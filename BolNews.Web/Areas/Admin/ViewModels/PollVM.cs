using System.ComponentModel.DataAnnotations;
using BolNews.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BolNews.Web.Areas.Admin.ViewModels;

public class PollVM
{
    public int Id { get; set; }
    [Required, StringLength(250)] public string Title { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    public PollType PollType { get; set; } = PollType.Choice;
    public int FeaturedMediaId { get; set; }
    public IFormFile? FeaturedImageUpload { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Voting deadline as an ISO-8601 UTC timestamp, produced in the browser from the admin's local time.</summary>
    public string? EndsAtUtc { get; set; }
    public List<PollOptionVM> Options { get; set; } = new();
    public List<SelectListItem> Images { get; set; } = new();
}

public class PollOptionVM
{
    public int Id { get; set; }
    [StringLength(150)] public string? Text { get; set; }
    /// <summary>Display only (admin sees real counts); never posted back.</summary>
    public int Votes { get; set; }
}

public class PollListItemVM
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public BolNews.Domain.Enums.PollType PollType { get; set; }
    public bool IsActive { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public int TotalVotes { get; set; }
    public List<(string Text, int Votes)> Options { get; set; } = new();
}
