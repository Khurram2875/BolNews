using System.ComponentModel.DataAnnotations;

namespace BolNews.Web.ViewModels.Account
{
    public class ForgotPasswordVM
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;
    }
}