namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class CategorySectionVM
    {
        public string CategoryName { get; set; }
        public string CategorySlug { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }

        public List<PublicArticleVM> Articles { get; set; } = new();
        public PublicArticleVM? FeaturedArticle { get; set; }
        public List<CategoryNavigationItemVM> RelatedCategories { get; set; } = new();
        public int Page { get; set; }
        public bool HasNextPage { get; set; }
        public string BaseUrl { get; set; }
        public string? CategorySchemaJson { get; set; }
        public string? BreadcrumbSchemaJson { get; set; }
    }

    public class CategoryNavigationItemVM
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
