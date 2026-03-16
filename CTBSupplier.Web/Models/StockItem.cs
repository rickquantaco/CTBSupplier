using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTBSupplier.Web.Models;

// PK is composite: (SupplierGUID, StockCode) — configured in DbContext via Fluent API
[Table("StockItem")]
public class StockItem
{
    // Part of composite PK and FK to Supplier.SupplierGUID
    public Guid SupplierGUID { get; set; }

    [Required]
    [MaxLength(250)]
    [Display(Name = "Stock Code")]
    public string StockCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    [Display(Name = "Description")]
    public string StockDesc { get; set; } = string.Empty;

    [MaxLength(255)]
    [Display(Name = "Brand Name")]
    public string? BrandName { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Supplier Stock Code")]
    public string SupplierStockCode { get; set; } = string.Empty;

    [Column(TypeName = "money")]
    [Display(Name = "Supplier Cost")]
    public decimal SupplierCost { get; set; }

    [Display(Name = "Stock Unit")]
    public double StockUnit { get; set; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "Unit of Measurement")]
    public string UnitOfMeasurementName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Display(Name = "Stock Category")]
    public string StockCategoryName { get; set; } = string.Empty;

    [Display(Name = "GST Applied")]
    public bool IsGstApplied { get; set; }

    [MaxLength(255)]
    [Display(Name = "Media URL")]
    public string? StockMediaUrl { get; set; }

    public Supplier? Supplier { get; set; }
}
