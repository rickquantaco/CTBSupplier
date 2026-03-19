using System.ComponentModel.DataAnnotations.Schema;

namespace CTBSupplier.Web.Models;

[Table("AppUserSupplier")]
public class AppUserSupplier
{
    public int AppUserId { get; set; }
    public Guid SupplierGUID { get; set; }

    public AppUser AppUser { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
}
