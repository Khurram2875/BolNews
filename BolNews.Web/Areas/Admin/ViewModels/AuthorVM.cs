using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        // NEW USER MODE
        [EmailAddress]
        public string? Email { get; set; }

        public string? Password { get; set; }

        // EXISTING USER MODE
        public bool LinkExistingUser { get; set; }

        public string? SelectedUserId { get; set; }

        // for dropdown
        public List<SelectListItem> ExistingUsers { get; set; } = new();
    }
}
