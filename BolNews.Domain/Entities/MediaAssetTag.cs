namespace BolNews.Domain.Entities;

public class MediaAssetTag
{
    public int MediaAssetId { get; set; }
    public MediaAsset MediaAsset { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
