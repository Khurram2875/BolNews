using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities.Base;
using BolNews.Domain.Enums;

namespace BolNews.Domain.Entities
{
    public class Article : BaseEntity
    {
        public string Title { get; set; }
        // ✅ SEO Fields
        public string Slug { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }

        public string Summary { get; set; }
        public string Content { get; set; }

        //public string? FeaturedImageUrl { get; set; }
        // Featured Images (Multi Size)
        public string? FeaturedImageThumb { get; set; }
        public string? FeaturedImageMedium { get; set; }
        public string? FeaturedImageLarge { get; set; }
        public string? FeaturedImageXl { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public int AuthorId { get; set; }
        public Author Author { get; set; }


        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReviewStartedAt { get; set; }
        public DateTime? FactCheckStartedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? ReviewEscalatedAt { get; set; }
        public DateTime? FactCheckEscalatedAt { get; set; }
        public DateTime? PublishEscalatedAt { get; set; }

        //Concurrent Editing Protection / Content Locking
        public string? LockedByUserId { get; set; }
        public DateTime? LockedAt { get; set; }

        public ArticleWorkflowStatus WorkflowStatus { get; set; }
        public string? WorkflowComment { get; set; }
        public string? ReviewerUserId { get; set; }
        public ApplicationUser? ReviewerUser { get; set; }

        public string? FactCheckerUserId { get; set; }
        public ApplicationUser? FactCheckerUser { get; set; }
        public int ViewCount { get; set; }

        #region Editorial Intelligence

        public bool IsEditorsPick { get; set; }

        public int EditorialPriority { get; set; }

        public bool IsFactChecked { get; set; }

        #endregion

        #region Intelligent Ranking Scores

        public decimal SeoScore { get; set; }

        public decimal EditorialScore { get; set; }

        public decimal EngagementScore { get; set; }

        public decimal FreshnessScore { get; set; }

        public decimal PopularityScore { get; set; }

        public decimal CredibilityScore { get; set; }

        public decimal OverallScore { get; set; }

        public DateTime? LastScoreCalculatedAt { get; set; }

        #endregion

        //Article Versioning
        public ICollection<ArticleRevision> Revisions { get; set; } = new List<ArticleRevision>();}
}
