namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class NotificationVM
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? Url { get; set; }
    }
}
