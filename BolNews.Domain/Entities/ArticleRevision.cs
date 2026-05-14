using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities.Base;

namespace BolNews.Domain.Entities
{
    public class ArticleRevision : BaseEntity
    {
        public int ArticleId { get; set; }
        public Article Article { get; set; }

        public int RevisionNumber { get; set; }

        public string Title { get; set; }
        public string Slug { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }

        public string Summary { get; set; }
        public string Content { get; set; }

        public string? FeaturedImageThumb { get; set; }
        public string? FeaturedImageMedium { get; set; }
        public string? FeaturedImageLarge { get; set; }
        public string? FeaturedImageXl { get; set; }

        public bool IsPublishedSnapshot { get; set; }

        public string ChangedByUserId { get; set; }

        public string WorkflowState { get; set; }

        public string? ChangeReason { get; set; }
    }
}
