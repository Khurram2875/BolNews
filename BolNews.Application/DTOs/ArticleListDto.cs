using BolNews.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public class ArticleListDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }

        public string? FeaturedImageThumb { get; set; }

        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategorySlug { get; set; }

        public int AuthorId { get; set; }
        public string? AuthorName { get; set; }

        public int? ReporterId { get; set; }
        public string? ReporterName { get; set; }

        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }

        public ArticleWorkflowStatus WorkflowStatus { get; set; }

        public string? ReviewerName { get; set; }
        public string? FactCheckerName { get; set; }

        public bool IsEditorsPick { get; set; }
        public int EditorialPriority { get; set; }
        public bool IsFactChecked { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
