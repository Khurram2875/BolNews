using BolNews.Domain.Enums;

namespace BolNews.Domain.Entities;

public class Poll
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PollType PollType { get; set; } = PollType.Choice;
    public int FeaturedMediaId { get; set; }
    public MediaAsset FeaturedMedia { get; set; } = null!;
    /// <summary>Hidden from the public site when false.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Voting deadline, stored in UTC.</summary>
    public DateTime EndsAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    public ICollection<PollVote> Votes { get; set; } = new List<PollVote>();
}

public class PollOption
{
    public int Id { get; set; }
    public int PollId { get; set; }
    public Poll Poll { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

/// <summary>One cast vote. VoterHash (from a long-lived cookie) is unique per poll; IpHash only caps abuse.</summary>
public class PollVote
{
    public int Id { get; set; }
    public int PollId { get; set; }
    public Poll Poll { get; set; } = null!;
    public int OptionId { get; set; }
    public PollOption Option { get; set; } = null!;
    public string VoterHash { get; set; } = string.Empty;
    public string IpHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
