namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class CategoryVM
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        // Optional (future SEO content)
        public string? Description { get; set; }
        public int? ParentCategoryId { get; set; }

        // For display
        public string? ParentCategoryName { get; set; }

        public bool IsDeleted { get; set; }

       
    }
}
