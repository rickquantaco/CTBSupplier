using Asp.Versioning;
using CTBSupplier.Web.Data;
using CTBSupplier.Web.Models.Api;
using CTBSupplier.Web.Services;
using Microsoft.AspNetCore.Authorization;
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
    /// </remarks>
    /// <param name="excludeAbn">Zero or more ABNs to exclude from the results.</param>
    [HttpGet]
    [Produces("application/json")]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetSuppliers(
        [FromQuery] List<string>? excludeAbn)
    {
        // Strip all spaces from incoming ABNs so they match the persisted computed column
        var exclusions = (excludeAbn ?? new List<string>())
            .Select(a => a.Replace(" ", ""))
            .Where(a => a.Length > 0)
            .ToList();

        IQueryable<Models.Supplier> query = _db.Suppliers.Where(s => s.IsActive);

        if (exclusions.Count > 0)
            query = query.Where(s => !exclusions.Contains(s.SupplierAbnForLookups!));

        var suppliers = await query
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
                StockItemCount      = _db.StockItems.Count(i => i.SupplierGUID == s.SupplierGUID)
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
                StockItemCount      = _db.StockItems.Count(i => i.SupplierGUID == s.SupplierGUID)
            })
            .FirstOrDefaultAsync();

        if (supplier is null)
            return NotFound();

        return Ok(supplier);
    }

    /// <summary>
    /// Returns stock items for a specific supplier, with optional filters.
    /// </summary>
    /// <remarks>
    /// Requires a valid API key supplied in the <c>X-API-Key</c> request header.
    ///
    /// Example:
    ///
    ///     GET /api/v1/suppliers/3fa85f64-5717-4562-b3fc-2c963f66afa6/stockitems
    ///     GET /api/v1/suppliers/3fa85f64-5717-4562-b3fc-2c963f66afa6/stockitems?stockCategoryName=Cleaning
    ///     GET /api/v1/suppliers/3fa85f64-5717-4562-b3fc-2c963f66afa6/stockitems?brandName=Dyson
    ///     GET /api/v1/suppliers/3fa85f64-5717-4562-b3fc-2c963f66afa6/stockitems?stockCategoryName=Cleaning&amp;brandName=Dyson
    /// </remarks>
    /// <param name="supplierGuid">The GUID of the supplier.</param>
    /// <param name="stockCategoryName">Optional: return only items in this stock category.</param>
    /// <param name="brandName">Optional: return only items with this brand name.</param>
    [HttpGet("{supplierGuid:guid}/stockitems")]
    [Produces("application/json")]
    public async Task<ActionResult<IEnumerable<StockItemDto>>> GetStockItems(
        Guid supplierGuid,
        [FromQuery] string? stockCategoryName,
        [FromQuery] string? brandName)
    {
        if (!await _db.Suppliers.AnyAsync(s => s.SupplierGUID == supplierGuid))
            return NotFound();

        IQueryable<Models.StockItem> query = _db.StockItems
            .Where(i => i.SupplierGUID == supplierGuid);

        if (!string.IsNullOrWhiteSpace(stockCategoryName))
            query = query.Where(i => i.StockCategoryName == stockCategoryName);

        if (!string.IsNullOrWhiteSpace(brandName))
            query = query.Where(i => i.BrandName == brandName);

        var items = await query
            .OrderBy(i => i.StockCode)
            .Select(i => new StockItemDto
            {
                SupplierGUID          = i.SupplierGUID,
                StockCode             = i.StockCode,
                StockDesc             = i.StockDesc,
                BrandName             = i.BrandName,
                SupplierStockCode     = i.SupplierStockCode,
                SupplierCost          = i.SupplierCost,
                StockUnit             = i.StockUnit,
                UnitOfMeasurementName = i.UnitOfMeasurementName,
                StockCategoryName     = i.StockCategoryName,
                IsGstApplied          = i.IsGstApplied,
                StockMediaUrl         = i.StockMediaUrl
            })
            .ToListAsync();

        return Ok(items);
    }
}
