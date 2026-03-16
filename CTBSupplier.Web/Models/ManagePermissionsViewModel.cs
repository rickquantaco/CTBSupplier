namespace CTBSupplier.Web.Models;

public class ManagePermissionsViewModel
{
    public int AppUserId { get; set; }
    public string UserRealName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;

    public List<PermissionCheckItem> Permissions { get; set; } = new();
}

public class PermissionCheckItem
{
    public int AppPermissionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
}
