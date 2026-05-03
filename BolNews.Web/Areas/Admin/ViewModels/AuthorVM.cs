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
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        //[Required]
        public string Password { get; set; }

        public string? NewPassword { get; set; }

        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
