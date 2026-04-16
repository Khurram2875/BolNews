namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class CategorySectionVM
    {
        public string CategoryName { get; set; }
        public string CategorySlug { get; set; }

        public List<PublicArticleVM> Articles { get; set; } = new();
    }
}
