namespace CTBSupplier.Web.Models;

public class SupplierDetailsViewModel
{
    public Supplier Supplier { get; set; } = null!;
    public IEnumerable<StockItem> StockItems { get; set; } = Enumerable.Empty<StockItem>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // Active filters
    public string? FilterBrand    { get; set; }
    public string? FilterCategory { get; set; }

    // Distinct values for this supplier — used to populate the filter dropdowns
    public IEnumerable<string> AvailableBrands     { get; set; } = Enumerable.Empty<string>();
    public IEnumerable<string> AvailableCategories { get; set; } = Enumerable.Empty<string>();

    public string ViewMode { get; set; } = "list";   // "list" | "grid"
}
