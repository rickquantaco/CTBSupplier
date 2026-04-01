namespace CTBSupplier.Web.Models.Api;

/// <summary>
/// A paged response envelope returned by list endpoints that support pagination.
/// </summary>
/// <typeparam name="T">The item type contained in <see cref="Data"/>.</typeparam>
public class PagedResult<T>
{
    /// <summary>The items in the current page.</summary>
    public IEnumerable<T> Data       { get; init; } = [];

    /// <summary>
    /// The total number of items matching the applied filters across all pages,
    /// regardless of the <c>start</c> / <c>pageSize</c> parameters.
    /// </summary>
    public int TotalCount { get; init; }
}
