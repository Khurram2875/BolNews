using System.ComponentModel.DataAnnotations;

namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class ReporterVM
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Source Name")]
        [StringLength(200)]
        public string? SourceName { get; set; }

        [StringLength(250)]
        public string? Slug { get; set; }

        public string DisplayName =>
            string.IsNullOrWhiteSpace(SourceName)
                ? Name
                : $"{Name} ({SourceName})";
    }
}
