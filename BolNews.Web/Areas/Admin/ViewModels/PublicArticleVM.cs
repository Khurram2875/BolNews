namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class PublicArticleVM
    {
        public string Title { get; set; }
        public string Slug { get; set; }

        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }

        public string Summary { get; set; }
        public string Content { get; set; }

        public string? FeaturedImageUrl { get; set; }
        public string? FeaturedImageThumb { get; set; }
        public string? FeaturedImageMedium { get; set; }
        public string? FeaturedImageLarge { get; set; }

        public string CategoryName { get; set; }
        public string CategorySlug { get; set; }

        public string AuthorName { get; set; }

        public DateTime? PublishedAt { get; set; }
    }
}
