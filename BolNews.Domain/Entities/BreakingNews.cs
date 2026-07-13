using BolNews.Domain.Entities.Base;
using BolNews.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Domain.Entities
{
    public class BreakingNews : BaseEntity
    {
        public int Id { get; set; }

        [Required]
        [StringLength(300)]
        public string Text { get; set; } = string.Empty;

        // Optional Article
        public int? ArticleId { get; set; }

        public Article? Article { get; set; }

        // Whether ticker should appear
        public bool IsActive { get; set; } = true;

        // Display order
        public int DisplayOrder { get; set; }

        // Optional scheduling
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        // Audit
        public string CreatedByUserId { get; set; } = string.Empty;

        public ApplicationUser? CreatedByUser { get; set; }

        public string? UpdatedByUserId { get; set; }

        public ApplicationUser? UpdatedByUser { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public TickerStyle TickerStyle { get; set; } = TickerStyle.Breaking;
        public bool IsPinned { get; set; } = false;
    }
}
