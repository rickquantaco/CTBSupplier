namespace CTBSupplier.Web.Models.Api;

public class StockItemDto
{
    public Guid    SupplierGUID          { get; init; }
    public string  StockCode             { get; init; } = string.Empty;
    public string  StockDesc             { get; init; } = string.Empty;
    public string? BrandName             { get; init; }
    public string  SupplierStockCode     { get; init; } = string.Empty;
    public decimal SupplierCost          { get; init; }
    public double  StockUnit             { get; init; }
    public string  UnitOfMeasurementName { get; init; } = string.Empty;
    public string  StockCategoryName     { get; init; } = string.Empty;
    public bool     IsGstApplied          { get; init; }
    public string?  StockMediaUrl         { get; init; }
    public DateTime DateAddedUtc          { get; init; }
}
