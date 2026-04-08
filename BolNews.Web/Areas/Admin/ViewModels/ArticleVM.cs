namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class ArticleVM
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public string? FeaturedImageUrl { get; set; }

        public int CategoryId { get; set; }
        public int AuthorId { get; set; }

        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }

        // Optional: Display fields for UI convenience
        public string AuthorName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }
}
