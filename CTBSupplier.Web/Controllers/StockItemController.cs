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
    public async Task<IActionResult> Index(int page = 1, int pageSize = 25)
    {
        // Clamp pageSize to allowed values
        if (pageSize != 25 && pageSize != 50 && pageSize != 100)
            pageSize = 25;
        if (page < 1) page = 1;

        var restriction = await _access.GetRestrictedSupplierGuidAsync(User);

        IQueryable<StockItem> query = _db.StockItems.Include(i => i.Supplier);

        if (restriction != null)
            query = query.Where(i => i.SupplierGUID == restriction.Value);

        query = query.OrderBy(i => i.StockCode);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new StockItemIndexViewModel
        {
            Items     = items,
            Page      = page,
            PageSize  = pageSize,
            TotalCount = total
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
            .FirstOrDefaultAsync(i => i.SupplierGUID == supplierGuid && i.StockCode == stockCode);
        if (item == null) return NotFound();
        return View(item);
    }

    // GET: /StockItem/Create?supplierGuid=...
    public async Task<IActionResult> Create(Guid? supplierGuid)
    {
        var restriction = await _access.GetRestrictedSupplierGuidAsync(User);

        // If restricted, enforce or auto-apply the supplier
        if (restriction != null)
        {
            if (supplierGuid.HasValue && supplierGuid.Value != restriction.Value)
                return View("Forbidden");
            supplierGuid = restriction.Value;
        }

        await PopulateSupplierDropdownAsync(supplierGuid);
        return View(new StockItem { SupplierGUID = supplierGuid ?? Guid.Empty });
    }

    // POST: /StockItem/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockItem item)
    {
        if (!await _access.CanAccessSupplierAsync(User, item.SupplierGUID))
            return View("Forbidden");

        if (!ModelState.IsValid)
        {
            await PopulateSupplierDropdownAsync(item.SupplierGUID);
            return View(item);
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
            .FirstOrDefaultAsync(i => i.SupplierGUID == supplierGuid && i.StockCode == stockCode);
        if (item == null) return NotFound();
        await PopulateSupplierDropdownAsync(item.SupplierGUID);
        return View(item);
    }

    // POST: /StockItem/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid supplierGuid, string stockCode, StockItem item)
    {
        if (supplierGuid != item.SupplierGUID || stockCode != item.StockCode) return BadRequest();
        if (!await _access.CanAccessSupplierAsync(User, supplierGuid))
            return View("Forbidden");
        if (!ModelState.IsValid)
        {
            await PopulateSupplierDropdownAsync(item.SupplierGUID);
            return View(item);
        }
        _db.Update(item);
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
        var suppliers = await _db.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.SupplierName)
            .ToListAsync();
        ViewBag.Suppliers = new SelectList(suppliers, "SupplierGUID", "SupplierName", selectedId);
    }
}
