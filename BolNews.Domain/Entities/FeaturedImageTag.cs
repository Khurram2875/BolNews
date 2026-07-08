namespace BolNews.Domain.Entities
{
    public class FeaturedImageTag
    {
        public int FeaturedImageMetadataId { get; set; }

        public FeaturedImageMetadata FeaturedImageMetadata { get; set; } = null!;

        public int TagId { get; set; }

        public Tag Tag { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public string? CreatedBy { get; set; }
    }
}
