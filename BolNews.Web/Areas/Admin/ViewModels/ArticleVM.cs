using System.ComponentModel.DataAnnotations;
using BolNews.Application.DTOs;
using BolNews.Domain.Enums;
using BolNews.Web.Models;

namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class ArticleVM : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        [StringLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string MetaTitle { get; set; }

        [Required]
        [StringLength(500)]
        public string MetaDescription { get; set; }

        [Required]
        [StringLength(1000)]
        public string Summary { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public IFormFile? ImageFile { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
        public int CategoryId { get; set; }
        public int AuthorId { get; set; }

        [Required]
        public int? ReporterId { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public bool SubmitForReview { get; set; }
        public bool IsEditorsPick { get; set; }
        public int EditorialPriority { get; set; }
        public bool IsFactChecked { get; set; }
        public ArticleWorkflowStatus WorkflowStatus { get; set; }
        public string? ReviewerName { get; set; }
        public string? FactCheckerName { get; set; }
        public string? WorkflowComment { get; set; }

        // ✅ REQUIRED FOR ADMIN LIST VIEW
        public string? FeaturedImageXl { get; set; }
        public string? FeaturedImageThumb { get; set; }
        public string? FeaturedImageMedium { get; set; }
        public string? FeaturedImageLarge { get; set; }
        public string? FeaturedImageAltText { get; set; }
        public string? FeaturedImageCaption { get; set; }
        public string? FeaturedImageCredit { get; set; }
        public int? FeaturedMediaId { get; set; }

        public string AuthorName { get; set; } = string.Empty;
        public string? ReporterName { get; set; }
        public string? ReporterSourceName { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DiscoverScoreResult? DiscoverScore { get; set; }
        //discussion comments for the article
        public List<ArticleDiscussionCommentDto> DiscussionComments { get; set; } = new();

        public string? NewDiscussionComment { get; set; }

        // Schedule Publishing /Emabrgo
        public DateTime? ScheduledPublishAt { get; set; }
        public DateTime? EmbargoUntil { get; set; }
        public string? ArticleTagsInput { get; set; }
        public string? FeaturedImageTagsInput { get; set; }
        public List<TagDto> ArticleTags { get; set; } = new();
        public List<TagDto> FeaturedImageTags { get; set; } = new();
        
        public IEnumerable<ValidationResult> Validate( ValidationContext validationContext)
        {
            var hasNewUpload = ImageFile != null && ImageFile.Length > 0;
            var hasMediaLibrarySelection = FeaturedMediaId.HasValue;
            var hasExistingImage = !string.IsNullOrWhiteSpace(FeaturedImageLarge);

            if (!hasNewUpload && !hasMediaLibrarySelection && !hasExistingImage)
            {
                yield return new ValidationResult(
                    "Please select a featured image — either upload a file or choose one from the Media Library.",
                    new[] { nameof(ImageFile) });
            }

            if (string.IsNullOrWhiteSpace(Content) ||
                string.Equals(Content.Trim(), "<p><br></p>", StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationResult(
                    "Please enter article content.",
                    new[] { nameof(Content) });
            }


            if (ScheduledPublishAt.HasValue &&
                ScheduledPublishAt <= DateTime.UtcNow)
            {
                yield return new ValidationResult(
                    "Scheduled publish must be in the future.",
                    new[] { nameof(ScheduledPublishAt) });
            }

            if (EmbargoUntil.HasValue &&
                EmbargoUntil <= DateTime.UtcNow)
            {
                yield return new ValidationResult(
                    "Embargo must be in the future.",
                    new[] { nameof(EmbargoUntil) });
            }
        }
    }
}
