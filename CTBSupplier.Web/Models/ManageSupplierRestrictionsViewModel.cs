namespace CTBSupplier.Web.Models;

public class ManageSupplierRestrictionsViewModel
{
    public int AppUserId { get; set; }
    public string UserRealName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;

    public List<SupplierCheckItem> Suppliers { get; set; } = new();
}

public class SupplierCheckItem
{
    public Guid SupplierGUID { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public bool IsRestricted { get; set; }
}
