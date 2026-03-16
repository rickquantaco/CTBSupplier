using CTBSupplier.Web.Data;
using CTBSupplier.Web.Models;
using CTBSupplier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTBSupplier.Web.Controllers;

[Authorize]
public class AppPermissionController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly PermissionService _permissions;

    public AppPermissionController(ApplicationDbContext db, PermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    // GET: /AppPermission
    public async Task<IActionResult> Index()
    {
        if (!await CurrentUserIsAdminAsync())
            return View("Forbidden");

        var permissions = await _db.AppPermissions
            .OrderBy(p => p.Description)
            .ToListAsync();
        return View(permissions);
    }

    // GET: /AppPermission/Create
    public async Task<IActionResult> Create()
    {
        if (!await CurrentUserIsAdminAsync())
            return View("Forbidden");

        return View(new AppPermission());
    }

    // POST: /AppPermission/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AppPermission permission)
    {
        if (!await CurrentUserIsAdminAsync())
            return View("Forbidden");

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
        if (!await CurrentUserIsAdminAsync())
            return View("Forbidden");

        var permission = await _db.AppPermissions.FindAsync(id);
        if (permission == null) return NotFound();
        return View(permission);
    }

    // POST: /AppPermission/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AppPermission permission)
    {
        if (!await CurrentUserIsAdminAsync())
            return View("Forbidden");

        if (id != permission.AppPermissionId) return BadRequest();
        if (!ModelState.IsValid) return View(permission);

        _db.Update(permission);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /AppPermission/Delete/{id}
    public async Task<IActionResult> Delete(int id)
    {
        if (!await CurrentUserIsAdminAsync())
            return View("Forbidden");

        var permission = await _db.AppPermissions.FindAsync(id);
        if (permission == null) return NotFound();
        return View(permission);
    }

    // POST: /AppPermission/Delete/{id}
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!await CurrentUserIsAdminAsync())
            return View("Forbidden");

        var permission = await _db.AppPermissions.FindAsync(id);
        if (permission != null)
        {
            _db.AppPermissions.Remove(permission);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // -------------------------------------------------------------------------
    private async Task<bool> CurrentUserIsAdminAsync()
    {
        var email = User.FindFirst("preferred_username")?.Value
                 ?? User.FindFirst("email")?.Value
                 ?? User.Identity?.Name
                 ?? string.Empty;

        return await _permissions.UserHasPermissionAsync(email, PermissionNames.AddUpdateAppUsers);
    }
}
