namespace CTBSupplier.Web.Models;

public class UrlCheckResult
{
    public string StockCode    { get; set; } = string.Empty;
    public Guid   SupplierGUID { get; set; }
    public string StockDesc    { get; set; } = string.Empty;
    public string Url          { get; set; } = string.Empty;

    // Null when a network/DNS exception occurred before a response was received
    public int?   StatusCode   { get; set; }
    public string? ErrorMessage { get; set; }

    public bool IsSuccess => StatusCode.HasValue && StatusCode >= 200 && StatusCode < 300;
}

public class ValidateStockItemUrlsViewModel
{
    public Supplier Supplier { get; set; } = null!;

    // Only items whose HEAD check returned non-2xx or threw an exception
    public List<UrlCheckResult> FailedResults { get; set; } = new();

    public int TotalWithUrl    { get; set; }   // items that had a URL and were checked
    public int TotalWithoutUrl { get; set; }   // items that had no URL (not checked)
}
