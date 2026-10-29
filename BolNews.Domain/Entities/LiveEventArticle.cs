namespace BolNews.Domain.Entities;

public class LiveEventArticle
{
    public int LiveEventId { get; set; }
    public LiveEvent LiveEvent { get; set; } = null!;
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
}
