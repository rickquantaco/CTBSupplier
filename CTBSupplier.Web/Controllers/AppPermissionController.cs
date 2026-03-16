using CTBSupplier.Web.Data;
using CTBSupplier.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTBSupplier.Web.Controllers;

[Authorize]
public class AppPermissionController : Controller
{
    private readonly ApplicationDbContext _db;

    public AppPermissionController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /AppPermission
    public async Task<IActionResult> Index()
    {
        var permissions = await _db.AppPermissions
            .OrderBy(p => p.Description)
            .ToListAsync();
        return View(permissions);
    }

    // GET: /AppPermission/Create
    public IActionResult Create() => View(new AppPermission());

    // POST: /AppPermission/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AppPermission permission)
    {
        if (!ModelState.IsValid) return View(permission);

        var exists = await _db.AppPermissions.AnyAsync(p => p.Description == permission.Description);
        if (exists)
        {
            ModelState.AddModelError(nameof(permission.Description), "A permission with this description already exists.");
            return View(permission);
        }

        _db.AppPermissions.Add(permission);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /AppPermission/Edit/{id}
    public async Task<IActionResult> Edit(int id)
    {
        var permission = await _db.AppPermissions.FindAsync(id);
        if (permission == null) return NotFound();
        return View(permission);
    }

    // POST: /AppPermission/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AppPermission permission)
    {
        if (id != permission.AppPermissionId) return BadRequest();
        if (!ModelState.IsValid) return View(permission);

        _db.Update(permission);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /AppPermission/Delete/{id}
    public async Task<IActionResult> Delete(int id)
    {
        var permission = await _db.AppPermissions.FindAsync(id);
        if (permission == null) return NotFound();
        return View(permission);
    }

    // POST: /AppPermission/Delete/{id}
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var permission = await _db.AppPermissions.FindAsync(id);
        if (permission != null)
        {
            _db.AppPermissions.Remove(permission);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
