using BolNews.Application.DTOs;

namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class EditorialDashboardVM
    {
        public List<EditorialQueueDto> Submitted { get; set; } = new();

        public List<EditorialQueueDto> UnderReview { get; set; } = new();

        public List<EditorialQueueDto> FactCheckPending { get; set; } = new();

        public List<EditorialQueueDto> Approved { get; set; } = new();

        public int PublishedToday { get; set; }
    }
}
