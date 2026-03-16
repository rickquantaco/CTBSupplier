using CTBSupplier.Web.Data;
using CTBSupplier.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTBSupplier.Web.Controllers;

[Authorize]
public class SupplierController : Controller
{
    private readonly ApplicationDbContext _db;

    public SupplierController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Supplier
    public async Task<IActionResult> Index()
    {
        var suppliers = await _db.Suppliers
            .OrderBy(s => s.SupplierName)
            .ToListAsync();
        return View(suppliers);
    }

    // GET: /Supplier/Details/{id}
    public async Task<IActionResult> Details(
        Guid id, int page = 1, int pageSize = 25,
        string? filterBrand = null, string? filterCategory = null)
    {
        if (pageSize != 25 && pageSize != 50 && pageSize != 100) pageSize = 25;
        if (page < 1) page = 1;

        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.SupplierGUID == id);
        if (supplier == null) return NotFound();

        // Load distinct brands and categories for this supplier (for the filter dropdowns)
        var allItems = _db.StockItems.Where(i => i.SupplierGUID == id);

        var availableBrands = (await allItems
            .Where(i => i.BrandName != null)
            .Select(i => i.BrandName!)
            .Distinct()
            .ToListAsync())
            .OrderBy(b => b, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var availableCategories = (await allItems
            .Select(i => i.StockCategoryName)
            .Distinct()
            .ToListAsync())
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Apply filters then page
        var query = allItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filterBrand))
            query = query.Where(i => i.BrandName == filterBrand);

        if (!string.IsNullOrWhiteSpace(filterCategory))
            query = query.Where(i => i.StockCategoryName == filterCategory);

        query = query.OrderBy(i => i.StockCode);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new SupplierDetailsViewModel
        {
            Supplier            = supplier,
            StockItems          = items,
            Page                = page,
            PageSize            = pageSize,
            TotalCount          = total,
            FilterBrand         = filterBrand,
            FilterCategory      = filterCategory,
            AvailableBrands     = availableBrands,
            AvailableCategories = availableCategories
        };

        return View(vm);
    }

    // GET: /Supplier/Create
    public IActionResult Create() => View(new Supplier());

    // POST: /Supplier/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Supplier supplier)
    {
        if (!ModelState.IsValid) return View(supplier);
        supplier.SupplierGUID = Guid.NewGuid();
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Supplier/Edit/{id}
    public async Task<IActionResult> Edit(Guid id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();
        return View(supplier);
    }

    // POST: /Supplier/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Supplier supplier)
    {
        if (id != supplier.SupplierGUID) return BadRequest();
        if (!ModelState.IsValid) return View(supplier);

        _db.Update(supplier);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Supplier/Delete/{id}
    public async Task<IActionResult> Delete(Guid id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();
        return View(supplier);
    }

    // POST: /Supplier/Delete/{id}
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier != null)
        {
            _db.Suppliers.Remove(supplier);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
