using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTBSupplier.Web.Models;

[Table("Supplier")]
public class Supplier
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid SupplierGUID { get; set; }

    [Required]
    [MaxLength(255)]
    [Display(Name = "Supplier Name")]
    public string SupplierName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Display(Name = "ABN")]
    public string SupplierAbn { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [MaxLength(255)]
    [Display(Name = "Website")]
    public string? Website { get; set; }

    [MaxLength(200)]
    [Display(Name = "Image URL")]
    public string? SupplierImage { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Description")]
    public string? SupplierDescription { get; set; }

    [MaxLength(20)]
    [Display(Name = "Category")]
    public string? SupplierCategory { get; set; }

    [Display(Name = "Date Added (UTC)")]
    public DateTime DateTimeAddedUtc { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public string? SupplierAbnForLookups { get; set; }

    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
}
