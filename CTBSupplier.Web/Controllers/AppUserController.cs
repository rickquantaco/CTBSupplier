using CTBSupplier.Web.Data;
using CTBSupplier.Web.Models;
using CTBSupplier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTBSupplier.Web.Controllers;

[Authorize]
public class AppUserController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly PermissionService _permissions;

    public AppUserController(ApplicationDbContext db, PermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    // GET: /AppUser
    public async Task<IActionResult> Index()
    {
        if (!await CurrentUserHasAddUpdatePermission())
            return View("Forbidden");

        var users = await _db.AppUsers
            .OrderBy(u => u.UserRealName)
            .ToListAsync();

        return View(users);
    }

    // GET: /AppUser/Create
    public async Task<IActionResult> Create()
    {
        if (!await CurrentUserHasAddUpdatePermission())
            return View("Forbidden");

        return View(new AppUser());
    }

    // POST: /AppUser/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AppUser user)
    {
        if (!await CurrentUserHasAddUpdatePermission())
            return View("Forbidden");

        if (!ModelState.IsValid)
            return View(user);

        var exists = await _db.AppUsers.AnyAsync(u => u.UserEmail.ToLower() == user.UserEmail.ToLower());
        if (exists)
        {
            ModelState.AddModelError(nameof(user.UserEmail), "This email address is already registered.");
            return View(user);
        }

        user.CreatedAt = DateTime.UtcNow;
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /AppUser/Edit/{id}
    public async Task<IActionResult> Edit(int id)
    {
        if (!await CurrentUserHasAddUpdatePermission())
            return View("Forbidden");

        var user = await _db.AppUsers.FindAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    // POST: /AppUser/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AppUser user)
    {
        if (!await CurrentUserHasAddUpdatePermission())
            return View("Forbidden");

        if (id != user.AppUserId) return BadRequest();
        if (!ModelState.IsValid)
            return View(user);

        _db.Update(user);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /AppUser/Delete/{id}
    public async Task<IActionResult> Delete(int id)
    {
        if (!await CurrentUserHasAddUpdatePermission())
            return View("Forbidden");

        var user = await _db.AppUsers.FindAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    // POST: /AppUser/Delete/{id}
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!await CurrentUserHasAddUpdatePermission())
            return View("Forbidden");

        var user = await _db.AppUsers.FindAsync(id);
        if (user != null)
        {
            _db.AppUsers.Remove(user);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: /AppUser/ManagePermissions/{id}
    public async Task<IActionResult> ManagePermissions(int id)
    {
        if (!await CurrentUserHasAddUpdatePermission())
            return View("Forbidden");
        var user = await _db.AppUsers
            .Include(u => u.AppUserPermissions)
            .FirstOrDefaultAsync(u => u.AppUserId == id);

        if (user == null) return NotFound();

        var allPermissions = await _db.AppPermissions.OrderBy(p => p.Description).ToListAsync();
        var grantedIds = user.AppUserPermissions.Select(up => up.AppPermissionId).ToHashSet();

        var vm = new ManagePermissionsViewModel
        {
            AppUserId    = user.AppUserId,
            UserRealName = user.UserRealName,
            UserEmail    = user.UserEmail,
            Permissions  = allPermissions.Select(p => new PermissionCheckItem
            {
                AppPermissionId = p.AppPermissionId,
                Description     = p.Description,
                IsGranted       = grantedIds.Contains(p.AppPermissionId)
            }).ToList()
        };

        return View(vm);
    }

    // POST: /AppUser/ManagePermissions/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManagePermissions(int id, ManagePermissionsViewModel vm)
    {
        if (!await CurrentUserHasAddUpdatePermission())
            return View("Forbidden");

        var user = await _db.AppUsers
            .Include(u => u.AppUserPermissions)
            .FirstOrDefaultAsync(u => u.AppUserId == id);

        if (user == null) return NotFound();

        _db.AppUserPermissions.RemoveRange(user.AppUserPermissions);

        foreach (var item in vm.Permissions.Where(p => p.IsGranted))
        {
            _db.AppUserPermissions.Add(new AppUserPermission
            {
                AppUserId       = id,
                AppPermissionId = item.AppPermissionId
            });
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /AppUser/ManageSupplierRestrictions/{id}
    public async Task<IActionResult> ManageSupplierRestrictions(int id)
    {
        if (!await CurrentUserHasAddUpdatePermission())
            return View("Forbidden");

        var user = await _db.AppUsers
            .Include(u => u.AppUserSuppliers)
            .FirstOrDefaultAsync(u => u.AppUserId == id);

        if (user == null) return NotFound();

        var allSuppliers = await _db.Suppliers.OrderBy(s => s.SupplierName).ToListAsync();
        var restrictedGuids = user.AppUserSuppliers.Select(s => s.SupplierGUID).ToHashSet();

        var vm = new ManageSupplierRestrictionsViewModel
        {
            AppUserId    = user.AppUserId,
            UserRealName = user.UserRealName,
            UserEmail    = user.UserEmail,
            Suppliers    = allSuppliers.Select(s => new SupplierCheckItem
            {
                SupplierGUID = s.SupplierGUID,
                SupplierName = s.SupplierName,
                IsRestricted = restrictedGuids.Contains(s.SupplierGUID)
            }).ToList()
        };

        return View(vm);
    }

    // POST: /AppUser/ManageSupplierRestrictions/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageSupplierRestrictions(int id, ManageSupplierRestrictionsViewModel vm)
    {
        if (!await CurrentUserHasAddUpdatePermission())
            return View("Forbidden");

        var user = await _db.AppUsers
            .Include(u => u.AppUserSuppliers)
            .FirstOrDefaultAsync(u => u.AppUserId == id);

        if (user == null) return NotFound();

        _db.AppUserSuppliers.RemoveRange(user.AppUserSuppliers);

        foreach (var item in vm.Suppliers.Where(s => s.IsRestricted))
        {
            _db.AppUserSuppliers.Add(new AppUserSupplier
            {
                AppUserId    = id,
                SupplierGUID = item.SupplierGUID
            });
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // -------------------------------------------------------------------------
    private async Task<bool> CurrentUserHasAddUpdatePermission()
    {
        var email = User.FindFirst("preferred_username")?.Value
                 ?? User.FindFirst("email")?.Value
                 ?? User.Identity?.Name
                 ?? string.Empty;

        return await _permissions.UserHasPermissionAsync(email, PermissionNames.AddUpdateAppUsers);
    }
}
