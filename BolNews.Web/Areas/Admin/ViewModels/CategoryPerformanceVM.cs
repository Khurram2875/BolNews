namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class CategoryPerformanceVM
    {
        public string CategoryName { get; set; }
        public int ArticleCount { get; set; }
        public int TotalViews { get; set; }
        public double AvgViewsPerArticle { get; set; }
    }
}
