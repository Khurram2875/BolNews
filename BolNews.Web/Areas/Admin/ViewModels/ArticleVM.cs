namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class ArticleVM
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }

        public string Summary { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public IFormFile? ImageFile { get; set; }

        public int CategoryId { get; set; }
        public int AuthorId { get; set; }

        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }

        // ✅ REQUIRED FOR ADMIN LIST VIEW
        //public string? FeaturedImageUrl { get; set; }
        public string? FeaturedImageThumb { get; set; }
        public string? FeaturedImageMedium { get; set; }
        public string? FeaturedImageLarge { get; set; }

        public string AuthorName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
