namespace BolNews.Domain.Entities;

public class LiveEventUpdate
{
    public int Id { get; set; }
    public int LiveEventId { get; set; }
    public LiveEvent LiveEvent { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}
