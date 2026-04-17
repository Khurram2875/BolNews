namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class CategorySectionVM
    {
        public string CategoryName { get; set; }
        public string CategorySlug { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }

        public List<PublicArticleVM> Articles { get; set; } = new();
        public string BaseUrl { get; set; }
        public string? SchemaJson { get; set; }
    }
}
