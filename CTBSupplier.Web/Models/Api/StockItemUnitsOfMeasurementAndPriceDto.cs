namespace CTBSupplier.Web.Models.Api;

public class StockItemUnitsOfMeasurementAndPriceDto
{
    public decimal SupplierCost          { get; init; }
    public double  StockUnit             { get; init; }
    public string  UnitOfMeasurementName { get; init; } = string.Empty;
    public string? Notes                 { get; init; }
}
