using BolNews.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BolNews.Web.Areas.Admin.ViewModels.BreakingNews
{
    public class BreakingNewsVM
    {
        public int Id { get; set; }

        [Display(Name = "Breaking News")]
        public string Text { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int DisplayOrder { get; set; }

        public int? ArticleId { get; set; }

        public string? ArticleTitle { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public string? UpdatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public TickerStyle TickerStyle { get; set; }
        public bool IsPinned { get; set; }
        public string Status
        {
            get
            {
                if (!IsActive)
                    return "Inactive";

                var now = DateTime.UtcNow;

                if (StartDate.HasValue && StartDate > now)
                    return "Scheduled";

                if (EndDate.HasValue && EndDate < now)
                    return "Expired";

                return "Active";
            }
        }
    }
}
