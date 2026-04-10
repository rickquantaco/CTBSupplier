using System.ComponentModel.DataAnnotations;

namespace CTBSupplier.Web.Models;

// Used by StockItem Create and Edit views instead of binding directly to the entity.
public class StockItemFormViewModel
{
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

    [Required]
    [MaxLength(255)]
    [Display(Name = "Stock Category")]
    public string StockCategoryName { get; set; } = string.Empty;

    [Display(Name = "GST Applied")]
    public bool IsGstApplied { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Media URL")]
    public string? StockMediaUrl { get; set; }

    // Populated on Edit only — used for display since the supplier cannot be changed.
    public string SupplierName { get; set; } = string.Empty;

    // At least one entry is always present (pre-populated with defaults).
    public List<PricingTierInput> PricingTiers { get; set; } = [new()];
}

// Not marked [Required] on UnitOfMeasurementName — blank rows are filtered server-side.
public class PricingTierInput
{
    [Display(Name = "Supplier Cost")]
    public decimal SupplierCost { get; set; }

    [Display(Name = "Stock Unit")]
    public double StockUnit { get; set; }

    [MaxLength(50)]
    [Display(Name = "Unit of Measurement")]
    public string UnitOfMeasurementName { get; set; } = string.Empty;
}
