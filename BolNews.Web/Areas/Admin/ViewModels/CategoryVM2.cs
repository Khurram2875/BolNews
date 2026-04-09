namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class CategoryVM2
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        public int? ParentCategoryId { get; set; }

        // For display
        public string? ParentCategoryName { get; set; }

        public bool IsDeleted { get; set; }

        public List<CategoryVM2> Children { get; set; } = new();
    }
}
