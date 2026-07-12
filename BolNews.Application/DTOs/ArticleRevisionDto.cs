namespace BolNews.Application.DTOs
{
    public class ArticleRevisionDto
    {
        public int Id { get; set; }

        public int ArticleId { get; set; }

        public int RevisionNumber { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string? MetaTitle { get; set; }

        public string? MetaDescription { get; set; }

        public string? Summary { get; set; }

        public string? Content { get; set; }

        public string? FeaturedImageThumb { get; set; }

        public string? FeaturedImageMedium { get; set; }

        public string? FeaturedImageLarge { get; set; }

        public string? FeaturedImageXl { get; set; }

        public bool IsPublishedSnapshot { get; set; }

        public string ChangedByUserId { get; set; } = string.Empty;

        public string WorkflowState { get; set; } = string.Empty;

        public string? ChangeReason { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
