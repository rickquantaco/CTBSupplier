using Asp.Versioning;
using CTBSupplier.Web.Data;
using CTBSupplier.Web.Models.Api;
using CTBSupplier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTBSupplier.Web.Controllers.Api;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1)]
[Authorize(AuthenticationSchemes = ApiKeyDefaults.AuthenticationScheme)]
public class SuppliersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SuppliersController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns all active suppliers with their stock item counts.
    /// Suppliers whose ABN appears in <paramref name="excludeAbn"/> are omitted.
    /// </summary>
    /// <remarks>
    /// Requires a valid API key supplied in the <c>X-API-Key</c> request header.
    /// Only active suppliers (IsActive = true) are returned.
    ///
    /// Example:
    ///
    ///     GET /api/v1/suppliers
    ///     GET /api/v1/suppliers?excludeAbn=51824753556
    ///     GET /api/v1/suppliers?excludeAbn=51824753556&amp;excludeAbn=12345678901
    ///     GET /api/v1/suppliers?addedAfterUtc=2026-04-01T00:00:00Z
    ///     GET /api/v1/suppliers?start=0&amp;pageSize=25
    /// </remarks>
    /// <param name="excludeAbn">Zero or more ABNs to exclude from the results.</param>
    /// <param name="addedAfterUtc">Optional: only return suppliers added strictly after this UTC date/time.</param>
    /// <param name="start">Zero-based offset into the sorted result set (default: 0). Results are sorted by supplier name then GUID.</param>
    /// <param name="pageSize">Maximum number of results to return. Omit to return all matching suppliers.</param>
    [HttpGet]
    [Produces("application/json")]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetSuppliers(
        [FromQuery] List<string>? excludeAbn,
        [FromQuery] DateTime? addedAfterUtc,
        [FromQuery] int start = 0,
        [FromQuery] int? pageSize = null)
    {
        // Strip all spaces from incoming ABNs so they match the persisted computed column
        var exclusions = (excludeAbn ?? new List<string>())
            .Select(a => a.Replace(" ", ""))
            .Where(a => a.Length > 0)
            .ToList();

        IQueryable<Models.Supplier> query = _db.Suppliers.Where(s => s.IsActive);

        if (exclusions.Count > 0)
            query = query.Where(s => !exclusions.Contains(s.SupplierAbnForLookups!));

        if (addedAfterUtc.HasValue)
            query = query.Where(s => s.DateTimeAddedUtc > addedAfterUtc.Value);

        IQueryable<Models.Supplier> pagedQuery = query
            .OrderBy(s => s.SupplierName)
            .ThenBy(s => s.SupplierGUID)
            .Skip(start);

        if (pageSize.HasValue && pageSize.Value > 0)
            pagedQuery = pagedQuery.Take(pageSize.Value);

        var suppliers = await pagedQuery
            .Select(s => new SupplierDto
            {
                SupplierGUID        = s.SupplierGUID,
                SupplierName        = s.SupplierName,
                SupplierAbn         = s.SupplierAbn,
                IsActive            = s.IsActive,
                Website             = s.Website,
                SupplierImage       = s.SupplierImage,
                SupplierDescription = s.SupplierDescription,
                SupplierCategory    = s.SupplierCategory,
                StockItemCount      = _db.StockItems.Count(i => i.SupplierGUID == s.SupplierGUID),
                DateTimeAddedUtc    = s.DateTimeAddedUtc
            })
            .ToListAsync();

        return Ok(suppliers);
    }

    /// <summary>
    /// Returns a single active supplier by their GUID, including stock item count.
    /// </summary>
    /// <remarks>
    /// Requires a valid API key supplied in the <c>X-API-Key</c> request header.
    /// Returns 404 if the supplier does not exist or is inactive.
    ///
    /// Example:
    ///
    ///     GET /api/v1/suppliers/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// </remarks>
    /// <param name="supplierGuid">The GUID of the supplier.</param>
    [HttpGet("{supplierGuid:guid}")]
    [Produces("application/json")]
    public async Task<ActionResult<SupplierDto>> GetSupplier(Guid supplierGuid)
    {
        var supplier = await _db.Suppliers
            .Where(s => s.SupplierGUID == supplierGuid && s.IsActive)
            .Select(s => new SupplierDto
            {
                SupplierGUID        = s.SupplierGUID,
                SupplierName        = s.SupplierName,
                SupplierAbn         = s.SupplierAbn,
                IsActive            = s.IsActive,
                Website             = s.Website,
                SupplierImage       = s.SupplierImage,
                SupplierDescription = s.SupplierDescription,
                SupplierCategory    = s.SupplierCategory,
                StockItemCount      = _db.StockItems.Count(i => i.SupplierGUID == s.SupplierGUID),
                DateTimeAddedUtc    = s.DateTimeAddedUtc
            })
            .FirstOrDefaultAsync();

        if (supplier is null)
            return NotFound();

        return Ok(supplier);
    }

    /// <summary>
    /// Searches for active suppliers by ABN and/or SupplierGUID.
    /// </summary>
    /// <remarks>
    /// Requires a valid API key supplied in the <c>X-API-Key</c> request header.
    /// At least one of <paramref name="abn"/> or <paramref name="supplierGuid"/> must be provided.
    /// When both are supplied, both conditions must match (AND logic).
    /// Spaces in the ABN are ignored.
    /// Only active suppliers (IsActive = true) are returned.
    ///
    /// Example:
    ///
    ///     GET /api/v1/suppliers/search?abn=51824753556
    ///     GET /api/v1/suppliers/search?abn=51 824 753 556
    ///     GET /api/v1/suppliers/search?supplierGuid=3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///     GET /api/v1/suppliers/search?abn=51824753556&amp;supplierGuid=3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///     GET /api/v1/suppliers/search?abn=51824753556&amp;start=0&amp;pageSize=25
    /// </remarks>
    /// <param name="abn">ABN to search for (spaces are ignored).</param>
    /// <param name="supplierGuid">Supplier GUID to search for.</param>
    /// <param name="start">Zero-based offset into the sorted result set (default: 0). Results are sorted by supplier name then GUID.</param>
    /// <param name="pageSize">Maximum number of results to return. Omit to return all matching suppliers.</param>
    [HttpGet("search")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> SearchSuppliers(
        [FromQuery] string? abn,
        [FromQuery] Guid? supplierGuid,
        [FromQuery] int start = 0,
        [FromQuery] int? pageSize = null)
    {
        if (string.IsNullOrWhiteSpace(abn) && supplierGuid is null)
            return BadRequest("At least one of 'abn' or 'supplierGuid' must be provided.");

        var normalizedAbn = abn?.Replace(" ", "");

        IQueryable<Models.Supplier> query = _db.Suppliers.Where(s => s.IsActive);

        if (!string.IsNullOrEmpty(normalizedAbn))
            query = query.Where(s => s.SupplierAbnForLookups == normalizedAbn);

        if (supplierGuid is not null)
            query = query.Where(s => s.SupplierGUID == supplierGuid.Value);

        IQueryable<Models.Supplier> pagedQuery = query
            .OrderBy(s => s.SupplierName)
            .ThenBy(s => s.SupplierGUID)
            .Skip(start);

        if (pageSize.HasValue && pageSize.Value > 0)
            pagedQuery = pagedQuery.Take(pageSize.Value);

        var suppliers = await pagedQuery
            .Select(s => new SupplierDto
            {
                SupplierGUID        = s.SupplierGUID,
                SupplierName        = s.SupplierName,
                SupplierAbn         = s.SupplierAbn,
                IsActive            = s.IsActive,
                Website             = s.Website,
                SupplierImage       = s.SupplierImage,
                SupplierDescription = s.SupplierDescription,
                SupplierCategory    = s.SupplierCategory,
                StockItemCount      = _db.StockItems.Count(i => i.SupplierGUID == s.SupplierGUID),
                DateTimeAddedUtc    = s.DateTimeAddedUtc
            })
            .ToListAsync();

        return Ok(suppliers);
    }

    /// <summary>
    /// Returns stock items for a specific supplier, with optional filters and sorting.
    /// </summary>
    /// <remarks>
    /// Requires a valid API key supplied in the <c>X-API-Key</c> request header.
    /// Returns 404 if the supplier does not exist.
    ///
    /// The response is a paged envelope:
    ///
    ///     {
    ///       "data": [ { ... }, { ... } ],
    ///       "totalCount": 729
    ///     }
    ///
    /// <c>totalCount</c> is the total number of matching items across all pages (respecting any
    /// active filters), regardless of <c>start</c> and <c>pageSize</c>.
    ///
    /// Valid <c>sortBy</c> values: <c>stockCode</c> (default), <c>stockDesc</c>, <c>brandName</c>,
    /// <c>supplierStockCode</c>, <c>stockCategoryName</c>.
    ///
    /// Example:
    ///
    ///     GET /api/v1/suppliers/3fa85f64-5717-4562-b3fc-2c963f66afa6/stockitems
    ///     GET /api/v1/suppliers/3fa85f64-5717-4562-b3fc-2c963f66afa6/stockitems?stockCategoryName=Cleaning
    ///     GET /api/v1/suppliers/3fa85f64-5717-4562-b3fc-2c963f66afa6/stockitems?brandName=Dyson
    ///     GET /api/v1/suppliers/3fa85f64-5717-4562-b3fc-2c963f66afa6/stockitems?stockDesc=brush
    ///     GET /api/v1/suppliers/3fa85f64-5717-4562-b3fc-2c963f66afa6/stockitems?stockCategoryName=Cleaning&amp;brandName=Dyson
    ///     GET /api/v1/suppliers/3fa85f64-5717-4562-b3fc-2c963f66afa6/stockitems?start=0&amp;pageSize=50
    ///     GET /api/v1/suppliers/3fa85f64-5717-4562-b3fc-2c963f66afa6/stockitems?sortBy=stockDesc
    /// </remarks>
    /// <param name="supplierGuid">The GUID of the supplier.</param>
    /// <param name="stockCategoryName">Optional: return only items in this stock category.</param>
    /// <param name="brandName">Optional: return only items with this brand name.</param>
    /// <param name="stockDesc">Optional: return only items whose description contains this value (case-insensitive).</param>
    /// <param name="sortBy">
    /// Optional: field to sort results by. Accepted values: <c>stockCode</c> (default), <c>stockDesc</c>,
    /// <c>brandName</c>, <c>supplierStockCode</c>, <c>stockCategoryName</c>.
    /// </param>
    /// <param name="start">Zero-based offset into the sorted result set (default: 0).</param>
    /// <param name="pageSize">Maximum number of results to return. Omit to return all matching items.</param>
    /// <returns>A <see cref="PagedResult{T}"/> containing the page of stock items and the total matching count.</returns>
    [HttpGet("{supplierGuid:guid}/stockitems")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PagedResult<StockItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<StockItemDto>>> GetStockItems(
        Guid supplierGuid,
        [FromQuery] string? stockCategoryName,
        [FromQuery] string? brandName,
        [FromQuery] string? stockDesc,
        [FromQuery] string? sortBy,
        [FromQuery] int start = 0,
        [FromQuery] int? pageSize = null)
    {
        if (!await _db.Suppliers.AnyAsync(s => s.SupplierGUID == supplierGuid))
            return NotFound();

        IQueryable<Models.StockItem> query = _db.StockItems
            .Where(i => i.SupplierGUID == supplierGuid);

        if (!string.IsNullOrWhiteSpace(stockCategoryName))
            query = query.Where(i => i.StockCategoryName == stockCategoryName);

        if (!string.IsNullOrWhiteSpace(brandName))
            query = query.Where(i => i.BrandName == brandName);

        if (!string.IsNullOrWhiteSpace(stockDesc))
            query = query.Where(i => i.StockDesc != null && i.StockDesc.Contains(stockDesc));

        var totalCount = await query.CountAsync();

        IQueryable<Models.StockItem> pagedQuery = (sortBy?.ToLowerInvariant() switch
        {
            "stockdesc"         => query.OrderBy(i => i.StockDesc),
            "brandname"         => query.OrderBy(i => i.BrandName),
            "supplierstockcode" => query.OrderBy(i => i.SupplierStockCode),
            "stockcategoryname" => query.OrderBy(i => i.StockCategoryName),
            _                   => query.OrderBy(i => i.StockCode),
        }).Skip(start);

        if (pageSize.HasValue && pageSize.Value > 0)
            pagedQuery = pagedQuery.Take(pageSize.Value);

        var rawItems = await pagedQuery
            .Include(i => i.PricingTiers)
            .ToListAsync();

        var items = rawItems.Select(i => MapToDto(i)).ToList();

        return Ok(new PagedResult<StockItemDto> { Data = items, TotalCount = totalCount });
    }

    /// <summary>
    /// Returns stock items for a specific supplier added after a given UTC date.
    /// </summary>
    /// <remarks>
    /// Requires a valid API key supplied in the <c>X-API-Key</c> request header.
    /// Returns items where <c>dateAddedUTC</c> is strictly greater than <paramref name="minDateAddedUtc"/>.
    /// Returns 404 if the supplier does not exist.
    ///
    /// The response is a paged envelope:
    ///
    ///     {
    ///       "data": [ { ... }, { ... } ],
    ///       "totalCount": 42
    ///     }
    ///
    /// <c>totalCount</c> is the total number of items matching the date filter across all pages,
    /// regardless of <c>start</c> and <c>pageSize</c>.
    ///
    /// Example:
    ///
    ///     GET /api/v1/suppliers/3fa85f64-5717-4562-b3fc-2c963f66afa6/stockitems/since?minDateAddedUtc=2026-04-01T00:00:00Z
    ///     GET /api/v1/suppliers/3fa85f64-5717-4562-b3fc-2c963f66afa6/stockitems/since?minDateAddedUtc=2026-04-01T00:00:00Z&amp;start=0&amp;pageSize=50
    /// </remarks>
    /// <param name="supplierGuid">The GUID of the supplier.</param>
    /// <param name="minDateAddedUtc">Only return items added strictly after this UTC date/time.</param>
    /// <param name="start">Zero-based offset into the sorted result set (default: 0). Results are sorted by date added then stock code.</param>
    /// <param name="pageSize">Maximum number of results to return. Omit to return all matching items.</param>
    /// <returns>A <see cref="PagedResult{T}"/> containing the page of stock items and the total matching count.</returns>
    [HttpGet("{supplierGuid:guid}/stockitems/since")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PagedResult<StockItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<StockItemDto>>> GetStockItemsSince(
        Guid supplierGuid,
        [FromQuery] DateTime minDateAddedUtc,
        [FromQuery] int start = 0,
        [FromQuery] int? pageSize = null)
    {
        if (!await _db.Suppliers.AnyAsync(s => s.SupplierGUID == supplierGuid))
            return NotFound();

        IQueryable<Models.StockItem> query = _db.StockItems
            .Where(i => i.SupplierGUID == supplierGuid && i.DateAddedUtc > minDateAddedUtc);

        var totalCount = await query.CountAsync();

        IQueryable<Models.StockItem> pagedQuery = query
            .OrderBy(i => i.DateAddedUtc)
            .ThenBy(i => i.StockCode)
            .Skip(start);

        if (pageSize.HasValue && pageSize.Value > 0)
            pagedQuery = pagedQuery.Take(pageSize.Value);

        var rawItems = await pagedQuery
            .Include(i => i.PricingTiers)
            .ToListAsync();

        var items = rawItems.Select(i => MapToDto(i)).ToList();

        return Ok(new PagedResult<StockItemDto> { Data = items, TotalCount = totalCount });
    }

    /// <summary>
    /// Returns the image URL for a specific stock item.
    /// </summary>
    /// <remarks>
    /// Requires a valid API key supplied in the <c>X-API-Key</c> request header.
    /// Returns 404 if the stock item does not exist or has no image URL.
    ///
    /// Example:
    ///
    ///     GET /api/v1/suppliers/3fa85f64-5717-4562-b3fc-2c963f66afa6/stockitems/ABC123/imageurl
    /// </remarks>
    /// <param name="supplierGuid">The GUID of the supplier.</param>
    /// <param name="stockCode">The stock code of the item.</param>
    [HttpGet("{supplierGuid:guid}/stockitems/{stockCode}/imageurl")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(StockItemImageUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockItemImageUrlDto>> GetStockItemImageUrl(
        Guid supplierGuid, string stockCode)
    {
        var item = await _db.StockItems
            .Where(i => i.SupplierGUID == supplierGuid && i.StockCode == stockCode)
            .Select(i => new { i.SupplierGUID, i.StockCode, i.StockMediaUrl })
            .FirstOrDefaultAsync();

        if (item is null || string.IsNullOrEmpty(item.StockMediaUrl))
            return NotFound();

        return Ok(new StockItemImageUrlDto
        {
            SupplierGUID  = item.SupplierGUID,
            StockCode     = item.StockCode,
            StockMediaUrl = item.StockMediaUrl
        });
    }

    // Maps a StockItem (with PricingTiers loaded) to the extended-contract DTO.
    // Scalar fields always reflect the primary (SortOrder 0) tier for backward compatibility.
    // PricingTiers is null when there is only one tier; populated with all tiers when there are two or more.
    private static StockItemDto MapToDto(Models.StockItem i)
    {
        var tiers = i.PricingTiers.OrderBy(t => t.SortOrder).ToList();
        var primary = tiers.FirstOrDefault();

        return new StockItemDto
        {
            SupplierGUID          = i.SupplierGUID,
            StockCode             = i.StockCode,
            StockDesc             = i.StockDesc,
            BrandName             = i.BrandName,
            SupplierStockCode     = i.SupplierStockCode,
            SupplierCost          = primary?.SupplierCost          ?? 0m,
            StockUnit             = primary?.StockUnit             ?? 0d,
            UnitOfMeasurementName = primary?.UnitOfMeasurementName ?? string.Empty,
            StockCategoryName     = i.StockCategoryName,
            IsGstApplied          = i.IsGstApplied,
            StockMediaUrl         = i.StockMediaUrl != null && i.StockMediaUrl.Length > 250
                                        ? i.StockMediaUrl[..250]
                                        : i.StockMediaUrl,
            DateAddedUtc          = i.DateAddedUtc,
            PricingTiers          = tiers.Count > 1
                ? tiers.Select(t => new StockItemUnitsOfMeasurementAndPriceDto
                  {
                      SupplierCost          = t.SupplierCost,
                      StockUnit             = t.StockUnit,
                      UnitOfMeasurementName = t.UnitOfMeasurementName
                  }).ToList()
                : null
        };
    }
}
