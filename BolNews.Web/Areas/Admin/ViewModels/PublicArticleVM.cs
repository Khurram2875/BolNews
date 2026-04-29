namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class PublicArticleVM
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }

        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }

        public string Summary { get; set; }
        public string Content { get; set; }

        //public string? FeaturedImageUrl { get; set; }
        public string? FeaturedImageThumb { get; set; }
        public string? FeaturedImageMedium { get; set; }
        public string? FeaturedImageLarge { get; set; }
        public string? FeaturedImageXl { get; set; }

        public string CategoryName { get; set; }
        public string CategorySlug { get; set; }

        public string AuthorName { get; set; }
        public string AuthorSlug { get; set; } 
        public string AuthorImage { get; set; }
        public int ReadingTimeMinutes =>
    string.IsNullOrEmpty(Content)
        ? 0
        : Math.Max(1, Content.Split(' ').Length / 200);

        public DateTime? PublishedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string TimeAgo
        {
            get
            {
                if (!PublishedAt.HasValue)
                    return "";

                var timeSpan = DateTime.UtcNow - PublishedAt.Value;

                if (timeSpan.TotalSeconds < 60)
                    return $"{(int)timeSpan.TotalSeconds} seconds ago";

                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes} minutes ago";

                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours} hours ago";

                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays} days ago";

                if (timeSpan.TotalDays < 30)
                    return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";

                if (timeSpan.TotalDays < 365)
                    return $"{(int)(timeSpan.TotalDays / 30)} months ago";

                return $"{(int)(timeSpan.TotalDays / 365)} years ago";
            }
        }
       
    }
}
