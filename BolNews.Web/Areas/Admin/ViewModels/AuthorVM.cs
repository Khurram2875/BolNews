using System.ComponentModel.DataAnnotations;

namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class AuthorVM
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Bio { get; set; }

        public string? ProfileImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}
