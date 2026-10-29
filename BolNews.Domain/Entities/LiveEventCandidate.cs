using BolNews.Domain.Enums;

namespace BolNews.Domain.Entities;

/// <summary>
/// One candidate row on a US election scoreboard card. Percentage and votes are
/// entered by the editors exactly as they should be displayed; nothing is calculated.
/// </summary>
public class LiveEventCandidate
{
    public int Id { get; set; }
    public int LiveEventId { get; set; }
    public LiveEvent LiveEvent { get; set; } = null!;
    public ElectionParty Party { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public int Votes { get; set; }
    public int SortOrder { get; set; }
}
