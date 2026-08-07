namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class CategoryTreeVM
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public int? ParentCategoryId { get; set; }

        // For display
        public string? ParentCategoryName { get; set; }

        public bool IsDeleted { get; set; }

        public List<CategoryTreeVM> Children { get; set; } = new();
    }
}
