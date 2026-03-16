using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTBSupplier.Web.Models;

[Table("AppUser")]
public class AppUser
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AppUserId { get; set; }

    [Required]
    [MaxLength(200)]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string UserEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Display(Name = "Full Name")]
    public string UserRealName { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Restricted to Supplier")]
    public Guid? SupplierGUID { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AppUserPermission> AppUserPermissions { get; set; } = new List<AppUserPermission>();
}
