using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTBSupplier.Web.Models;

[Table("AppPermission")]
public class AppPermission
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AppPermissionId { get; set; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "Permission")]
    public string Description { get; set; } = string.Empty;

    public ICollection<AppUserPermission> AppUserPermissions { get; set; } = new List<AppUserPermission>();
}
