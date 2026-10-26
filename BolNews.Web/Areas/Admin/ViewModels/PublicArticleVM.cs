using BolNews.Application.DTOs;

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
        public string? FeaturedImageAltText { get; set; }
        public string? FeaturedImageCaption { get; set; }
        public string? FeaturedImageCredit { get; set; }

        public string CategoryName { get; set; }
        public string CategorySlug { get; set; }

        public string AuthorName { get; set; }
        public string AuthorSlug { get; set; } 
        public string AuthorImage { get; set; }
        public string? ReporterName { get; set; }
        public string? ReporterSourceName { get; set; }
        public List<TagDto> ArticleTags { get; set; } = new();
        public List<TagDto> FeaturedImageTags { get; set; } = new();
        public string Keywords => string.Join(", ", ArticleTags.Select(t => t.Name));
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
                {
                    var count = Math.Max(1, (int)timeSpan.TotalSeconds);
                    return $"{count} {(count == 1 ? "second" : "seconds")} ago";
                }

                if (timeSpan.TotalMinutes < 60)
                {
                    var count = (int)timeSpan.TotalMinutes;
                    return $"{count} {(count == 1 ? "minute" : "minutes")} ago";
                }

                if (timeSpan.TotalHours < 24)
                {
                    var count = (int)timeSpan.TotalHours;
                    return $"{count} {(count == 1 ? "hour" : "hours")} ago";
                }

                if (timeSpan.TotalDays < 7)
                {
                    var count = (int)timeSpan.TotalDays;
                    return $"{count} {(count == 1 ? "day" : "days")} ago";
                }

                if (timeSpan.TotalDays < 30)
                {
                    var count = (int)(timeSpan.TotalDays / 7);
                    return $"{count} {(count == 1 ? "week" : "weeks")} ago";
                }

                if (timeSpan.TotalDays < 365)
                {
                    var count = (int)(timeSpan.TotalDays / 30);
                    return $"{count} {(count == 1 ? "month" : "months")} ago";
                }

                var years = (int)(timeSpan.TotalDays / 365);
                return $"{years} {(years == 1 ? "year" : "years")} ago";
            }
        }
        public DashboardDto dashboard { get; set; }
       
    }
}
