namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class AssignRolesVM
    {
        public string UserId { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public List<RoleSelectionVM> Roles { get; set; } = new();
    }

    public class RoleSelectionVM
    {
        public string RoleName { get; set; } = string.Empty;

        public bool IsSelected { get; set; }
    }
}
