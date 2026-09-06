namespace BolNews.Web.Areas.Admin.ViewModels
{
   
        public class ArticleRowContext
        {
            public ArticleVM Article { get; set; } = null!;
            public int? PinnedTopStoryArticleId { get; set; }
            public Dictionary<int, int> SecondaryPlacementByArticleId { get; set; } = new();
            public Dictionary<int, int> LatestPlacementByArticleId { get; set; } = new();
            public Dictionary<int, int> FeaturedPlacementByArticleId { get; set; } = new();
            public string ReturnUrl { get; set; } = "";
            public bool CanManageHomepagePlacement { get; set; }
            public bool CanViewRevisionHistory { get; set; }
            public bool LatestPlacementLimitReached { get; set; }
            public bool FeaturedPlacementLimitReached { get; set; }
        }
    
}
