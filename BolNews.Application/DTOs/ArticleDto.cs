using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Enums;

namespace BolNews.Application.DTOs
{
    public class ArticleDto
    {
        public int Id { get; set; }

        public string Title { get; set; }
        public string Slug { get; set; }

        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }

        public string Summary { get; set; }
        public string Content { get; set; }

        public string? FeaturedImageXl { get; set; }
        public string? FeaturedImageThumb { get; set; }
        public string? FeaturedImageMedium { get; set; }
        public string? FeaturedImageLarge { get; set; }
        public string? FeaturedImageAltText { get; set; }
        public string? FeaturedImageCaption { get; set; }
        public string? FeaturedImageCredit { get; set; }


        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategorySlug { get; set; } // 🔥 ADD THIS

        public int AuthorId { get; set; }
        public string? AuthorName { get; set; }

        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }

        public ArticleWorkflowStatus WorkflowStatus { get; set; }
        public string? WorkflowComment { get; set; }
        public string? ReviewerName { get; set; }
        public string? FactCheckerName { get; set; }
        public bool SubmitForReview { get; set; }
        public bool IsEditorsPick { get; set; }
        public int EditorialPriority { get; set; }
        public bool IsFactChecked { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        // Schedule Publishing /Emabrgo
        public DateTime? ScheduledPublishAt { get; set; }
        public DateTime? EmbargoUntil { get; set; }

        public string? ArticleTagsInput { get; set; }
        public string? FeaturedImageTagsInput { get; set; }
        public List<TagDto> ArticleTags { get; set; } = new();
        public List<TagDto> FeaturedImageTags { get; set; } = new();

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
