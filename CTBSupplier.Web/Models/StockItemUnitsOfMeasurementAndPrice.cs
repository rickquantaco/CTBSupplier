using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTBSupplier.Web.Models;

[Table("StockItemUnitsOfMeasurementAndPrice")]
public class StockItemUnitsOfMeasurementAndPrice
{
    public int Id { get; set; }

    // FK to StockItem composite PK
    public Guid SupplierGUID { get; set; }

    [MaxLength(250)]
    public string StockCode { get; set; } = string.Empty;

    [Column(TypeName = "money")]
    [Display(Name = "Supplier Cost")]
    public decimal SupplierCost { get; set; }

    [Display(Name = "Stock Unit")]
    public double StockUnit { get; set; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "Unit of Measurement")]
    public string UnitOfMeasurementName { get; set; } = string.Empty;

    // Controls display order; 0 = primary tier
    public int SortOrder { get; set; }

    public StockItem? StockItem { get; set; }
}
