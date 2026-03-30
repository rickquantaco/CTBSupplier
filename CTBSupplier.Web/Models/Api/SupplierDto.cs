namespace CTBSupplier.Web.Models.Api;

public class SupplierDto
{
    public Guid   SupplierGUID   { get; init; }
    public string SupplierName   { get; init; } = string.Empty;
    public string SupplierAbn    { get; init; } = string.Empty;
    public bool   IsActive       { get; init; }
    public string? Website             { get; init; }
    public string? SupplierImage       { get; init; }
    public string? SupplierDescription { get; init; }
    public string? SupplierCategory    { get; init; }
    public int      StockItemCount      { get; init; }
    public DateTime DateTimeAddedUtc    { get; init; }
}
