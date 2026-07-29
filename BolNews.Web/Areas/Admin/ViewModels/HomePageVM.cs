namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class HomePageVM
    {
        public PublicArticleVM? TopStory { get; set; }

        public List<PublicArticleVM> SecondaryStories { get; set; } = new();
        public int PinnedSecondaryStoryCount { get; set; }

        public List<CategorySectionVM> CategorySections { get; set; } = new();
        public string? BaseUrl { get; set; }
    }
}
