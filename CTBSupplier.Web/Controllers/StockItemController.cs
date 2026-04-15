using CTBSupplier.Web.Data;
using CTBSupplier.Web.Models;
using CTBSupplier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CTBSupplier.Web.Controllers;

[Authorize]
public class StockItemController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly SupplierAccessService _access;

    public StockItemController(ApplicationDbContext db, SupplierAccessService access)
    {
        _db = db;
        _access = access;
    }

    // GET: /StockItem
    public async Task<IActionResult> Index(int page = 1, int pageSize = 25, string? viewMode = null)
    {
        // Clamp pageSize to allowed values
        if (pageSize != 25 && pageSize != 50 && pageSize != 100)
            pageSize = 25;
        if (page < 1) page = 1;

        // Resolve viewMode: explicit param → cookie → default "list"
        if (string.IsNullOrEmpty(viewMode))
            viewMode = Request.Cookies["StockItemViewMode"] ?? "list";
        if (viewMode != "grid") viewMode = "list";
        Response.Cookies.Append("StockItemViewMode", viewMode,
            new Microsoft.AspNetCore.Http.CookieOptions { MaxAge = TimeSpan.FromDays(365), IsEssential = true });

        var allowed = await _access.GetAllowedSupplierGuidsAsync(User);

        IQueryable<StockItem> query = _db.StockItems
            .Include(i => i.Supplier)
            .Include(i => i.PricingTiers);

        if (allowed != null)
            query = query.Where(i => allowed.Contains(i.SupplierGUID));

        query = query.OrderBy(i => i.StockCode);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new StockItemIndexViewModel
        {
            Items      = items,
            Page       = page,
            PageSize   = pageSize,
            TotalCount = total,
            ViewMode   = viewMode
        };

        return View(vm);
    }

    // GET: /StockItem/Details?supplierGuid=...&stockCode=...
    public async Task<IActionResult> Details(Guid supplierGuid, string stockCode)
    {
        if (!await _access.CanAccessSupplierAsync(User, supplierGuid))
            return View("Forbidden");

        var item = await _db.StockItems
            .Include(i => i.Supplier)
            .Include(i => i.PricingTiers)
            .FirstOrDefaultAsync(i => i.SupplierGUID == supplierGuid && i.StockCode == stockCode);
        if (item == null) return NotFound();
        return View(item);
    }

    // GET: /StockItem/Create?supplierGuid=...
    public async Task<IActionResult> Create(Guid? supplierGuid)
    {
        var allowed = await _access.GetAllowedSupplierGuidsAsync(User);

        // If restricted, enforce the supplier is within the allowed set
        if (allowed != null && supplierGuid.HasValue && !allowed.Contains(supplierGuid.Value))
            return View("Forbidden");

        await PopulateSupplierDropdownAsync(supplierGuid);
        return View(new StockItemFormViewModel { SupplierGUID = supplierGuid ?? Guid.Empty });
    }

    // POST: /StockItem/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockItemFormViewModel vm)
    {
        if (!await _access.CanAccessSupplierAsync(User, vm.SupplierGUID))
            return View("Forbidden");

        var validTiers = vm.PricingTiers
            .Where(t => !string.IsNullOrWhiteSpace(t.UnitOfMeasurementName))
            .ToList();

        if (validTiers.Count == 0)
            ModelState.AddModelError("PricingTiers", "At least one pricing tier with a Unit of Measurement is required.");

        if (!ModelState.IsValid)
        {
            await PopulateSupplierDropdownAsync(vm.SupplierGUID);
            return View(vm);
        }

        var item = new StockItem
        {
            SupplierGUID      = vm.SupplierGUID,
            StockCode         = vm.StockCode,
            StockDesc         = vm.StockDesc,
            BrandName         = vm.BrandName,
            SupplierStockCode = vm.SupplierStockCode,
            StockCategoryName = vm.StockCategoryName,
            IsGstApplied      = vm.IsGstApplied,
            StockMediaUrl     = vm.StockMediaUrl
        };

        for (int i = 0; i < validTiers.Count; i++)
        {
            item.PricingTiers.Add(new StockItemUnitsOfMeasurementAndPrice
            {
                SupplierGUID          = vm.SupplierGUID,
                StockCode             = vm.StockCode,
                SupplierCost          = validTiers[i].SupplierCost,
                StockUnit             = validTiers[i].StockUnit,
                UnitOfMeasurementName = validTiers[i].UnitOfMeasurementName,
                Notes                 = validTiers[i].Notes,
                SortOrder             = i
            });
        }

        _db.StockItems.Add(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Details", "Supplier", new { id = item.SupplierGUID });
    }

    // GET: /StockItem/Edit?supplierGuid=...&stockCode=...
    public async Task<IActionResult> Edit(Guid supplierGuid, string stockCode)
    {
        if (!await _access.CanAccessSupplierAsync(User, supplierGuid))
            return View("Forbidden");

        var item = await _db.StockItems
            .Include(i => i.Supplier)
            .Include(i => i.PricingTiers)
            .FirstOrDefaultAsync(i => i.SupplierGUID == supplierGuid && i.StockCode == stockCode);
        if (item == null) return NotFound();

        var vm = new StockItemFormViewModel
        {
            SupplierGUID      = item.SupplierGUID,
            SupplierName      = item.Supplier?.SupplierName ?? string.Empty,
            StockCode         = item.StockCode,
            StockDesc         = item.StockDesc,
            BrandName         = item.BrandName,
            SupplierStockCode = item.SupplierStockCode,
            StockCategoryName = item.StockCategoryName,
            IsGstApplied      = item.IsGstApplied,
            StockMediaUrl     = item.StockMediaUrl,
            PricingTiers      = item.PricingTiers
                .OrderBy(t => t.SortOrder)
                .Select(t => new PricingTierInput
                {
                    SupplierCost          = t.SupplierCost,
                    StockUnit             = t.StockUnit,
                    UnitOfMeasurementName = t.UnitOfMeasurementName,
                    Notes                 = t.Notes
                })
                .ToList()
        };

        if (vm.PricingTiers.Count == 0)
            vm.PricingTiers.Add(new PricingTierInput());

        return View(vm);
    }

    // POST: /StockItem/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid supplierGuid, string stockCode, StockItemFormViewModel vm)
    {
        if (supplierGuid != vm.SupplierGUID || stockCode != vm.StockCode) return BadRequest();
        if (!await _access.CanAccessSupplierAsync(User, supplierGuid))
            return View("Forbidden");

        var validTiers = vm.PricingTiers
            .Where(t => !string.IsNullOrWhiteSpace(t.UnitOfMeasurementName))
            .ToList();

        if (validTiers.Count == 0)
            ModelState.AddModelError("PricingTiers", "At least one pricing tier with a Unit of Measurement is required.");

        if (!ModelState.IsValid)
        {
            vm.SupplierName = (await _db.Suppliers.FindAsync(supplierGuid))?.SupplierName ?? string.Empty;
            return View(vm);
        }

        var item = await _db.StockItems
            .Include(i => i.PricingTiers)
            .FirstOrDefaultAsync(i => i.SupplierGUID == supplierGuid && i.StockCode == stockCode);
        if (item == null) return NotFound();

        // Update scalar fields
        item.StockDesc         = vm.StockDesc;
        item.BrandName         = vm.BrandName;
        item.SupplierStockCode = vm.SupplierStockCode;
        item.StockCategoryName = vm.StockCategoryName;
        item.IsGstApplied      = vm.IsGstApplied;
        item.StockMediaUrl     = vm.StockMediaUrl;

        // Replace all pricing tiers
        _db.StockItemUnitsOfMeasurementAndPrices.RemoveRange(item.PricingTiers);
        item.PricingTiers.Clear();

        for (int i = 0; i < validTiers.Count; i++)
        {
            item.PricingTiers.Add(new StockItemUnitsOfMeasurementAndPrice
            {
                SupplierGUID          = supplierGuid,
                StockCode             = stockCode,
                SupplierCost          = validTiers[i].SupplierCost,
                StockUnit             = validTiers[i].StockUnit,
                UnitOfMeasurementName = validTiers[i].UnitOfMeasurementName,
                Notes                 = validTiers[i].Notes,
                SortOrder             = i
            });
        }

        await _db.SaveChangesAsync();
        return RedirectToAction("Details", "Supplier", new { id = item.SupplierGUID });
    }

    // GET: /StockItem/Delete?supplierGuid=...&stockCode=...
    public async Task<IActionResult> Delete(Guid supplierGuid, string stockCode)
    {
        if (!await _access.CanAccessSupplierAsync(User, supplierGuid))
            return View("Forbidden");

        var item = await _db.StockItems
            .Include(i => i.Supplier)
            .Include(i => i.PricingTiers)
            .FirstOrDefaultAsync(i => i.SupplierGUID == supplierGuid && i.StockCode == stockCode);
        if (item == null) return NotFound();
        return View(item);
    }

    // POST: /StockItem/Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid supplierGuid, string stockCode)
    {
        if (!await _access.CanAccessSupplierAsync(User, supplierGuid))
            return View("Forbidden");

        var item = await _db.StockItems
            .FirstOrDefaultAsync(i => i.SupplierGUID == supplierGuid && i.StockCode == stockCode);
        if (item != null)
        {
            _db.StockItems.Remove(item);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Details", "Supplier", new { id = supplierGuid });
    }

    private async Task PopulateSupplierDropdownAsync(Guid? selectedId = null)
    {
        var allowed = await _access.GetAllowedSupplierGuidsAsync(User);
        var query   = _db.Suppliers.Where(s => s.IsActive);
        if (allowed != null)
            query = query.Where(s => allowed.Contains(s.SupplierGUID));
        var suppliers = await query.OrderBy(s => s.SupplierName).ToListAsync();
        ViewBag.Suppliers = new SelectList(suppliers, "SupplierGUID", "SupplierName", selectedId);
    }
}
