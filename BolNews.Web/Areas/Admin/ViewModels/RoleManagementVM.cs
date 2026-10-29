namespace BolNews.Web.Areas.Admin.ViewModels;

public sealed class RoleManagementVM
{
    public List<SupportedRoleVM> Roles { get; set; } = [];
}

public sealed class SupportedRoleVM
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCreated { get; set; }
    public int AssignedUserCount { get; set; }
}
