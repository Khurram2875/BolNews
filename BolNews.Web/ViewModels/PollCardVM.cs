namespace BolNews.Web.ViewModels;

/// <summary>Everything the poll card needs. Only percentages are exposed, never raw vote counts.</summary>
public class PollCardVM
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TypeLabel { get; set; } = "Poll";
    public bool IsVersus { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime EndsAtUtc { get; set; }
    public bool HasVoted { get; set; }
    public bool Ended => DateTime.UtcNow >= EndsAtUtc;
    public List<PollCardOptionVM> Options { get; set; } = new();
}

public class PollCardOptionVM
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Percent { get; set; }
    public int Index { get; set; }
}
