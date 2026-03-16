using CTBSupplier.Web.Data;
using CTBSupplier.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CTBSupplier.Web.Controllers;

[Authorize]
public class StockItemController : Controller
{
    private readonly ApplicationDbContext _db;

    public StockItemController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /StockItem
    public async Task<IActionResult> Index(int page = 1, int pageSize = 25)
    {
        // Clamp pageSize to allowed values
        if (pageSize != 25 && pageSize != 50 && pageSize != 100)
            pageSize = 25;
        if (page < 1) page = 1;

        var query = _db.StockItems
            .Include(i => i.Supplier)
            .OrderBy(i => i.StockCode);

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
        var item = await _db.StockItems
            .Include(i => i.Supplier)
            .FirstOrDefaultAsync(i => i.SupplierGUID == supplierGuid && i.StockCode == stockCode);
        if (item == null) return NotFound();
        return View(item);
    }

    // GET: /StockItem/Create?supplierGuid=...
    public async Task<IActionResult> Create(Guid? supplierGuid)
    {
        await PopulateSupplierDropdownAsync(supplierGuid);
        return View(new StockItem { SupplierGUID = supplierGuid ?? Guid.Empty });
    }

    // POST: /StockItem/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockItem item)
    {
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
