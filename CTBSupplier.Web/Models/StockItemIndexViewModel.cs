namespace CTBSupplier.Web.Models;

public class StockItemIndexViewModel
{
    public IEnumerable<StockItem> Items { get; set; } = Enumerable.Empty<StockItem>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
