using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Enums;

namespace BolNews.Application.DTOs
{
    public class EditorialQueueDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ArticleWorkflowStatus WorkflowStatus { get; set; }
        public string? ReviewerUserId { get; set; }
        public string? ReviewerName { get; set; }
        public string? FactCheckerUserId { get; set; }
        public string? FactCheckerName { get; set; }
        public decimal OverallScore { get; set; }
        public bool IsFactChecked { get; set; }
        public bool IsPublished { get; set; }
        public SlaStatusResult? SlaStatus { get; set; }
    }
}
