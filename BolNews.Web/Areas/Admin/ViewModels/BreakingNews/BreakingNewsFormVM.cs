using BolNews.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BolNews.Web.Areas.Admin.ViewModels.BreakingNews
{
    public class BreakingNewsFormVM
    {
        [Required]
        [StringLength(500)]
        [Display(Name = "Breaking News")]
        public string Text { get; set; } = string.Empty;

        [Display(Name = "Linked Article")]
        public int? ArticleId { get; set; }

        [Display(Name = "Ticker Style")]
        public TickerStyle TickerStyle { get; set; } = TickerStyle.Breaking;

        [Display(Name = "Pinned")]
        public bool IsPinned { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 100;

        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }
    }
}
