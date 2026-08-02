namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class EditorialPlacementIndexVM
    {
        public EditorialPlacementVM? TopStory { get; set; }

        public List<EditorialPlacementVM> SecondaryStories { get; set; } = new();
        public List<EditorialPlacementVM> LatestStories { get; set; } = new();
        public List<EditorialPlacementVM> FeaturedStories { get; set; } = new();
    }

    public class EditorialPlacementVM
    {
        public int PlacementId { get; set; }

        public int ArticleId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public DateTime? PublishedAt { get; set; }

        public int SortOrder { get; set; }
       
    }
}
