using System.ComponentModel.DataAnnotations.Schema;

namespace CTBSupplier.Web.Models;

// Intersection table: composite PK (AppUserId, AppPermissionId) configured via Fluent API
[Table("AppUserPermission")]
public class AppUserPermission
{
    public int AppUserId { get; set; }
    public int AppPermissionId { get; set; }

    public AppUser AppUser { get; set; } = null!;
    public AppPermission AppPermission { get; set; } = null!;
}
