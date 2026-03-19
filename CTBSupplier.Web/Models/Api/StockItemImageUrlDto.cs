namespace CTBSupplier.Web.Models.Api;

public class StockItemImageUrlDto
{
    public Guid SupplierGUID { get; init; }
    public string StockCode { get; init; } = string.Empty;
    public string? StockMediaUrl { get; init; }
}
