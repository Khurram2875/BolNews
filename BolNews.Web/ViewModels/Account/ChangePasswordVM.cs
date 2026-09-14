using System.ComponentModel.DataAnnotations;

namespace BolNews.Web.ViewModels.Account
{
    public class ChangePasswordVM
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(
            100,
            MinimumLength = 12,
            ErrorMessage =
                "Password must be at least 12 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(
            nameof(NewPassword),
            ErrorMessage =
                "The new password and confirmation password do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
