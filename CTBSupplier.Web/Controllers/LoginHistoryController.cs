using CTBSupplier.Web.Data;
using CTBSupplier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTBSupplier.Web.Controllers;

[Authorize]
public class LoginHistoryController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly PermissionService _permissions;

    public LoginHistoryController(ApplicationDbContext db, PermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    // GET: /LoginHistory
    public async Task<IActionResult> Index()
    {
        if (!await CurrentUserIsAdminAsync())
            return View("Forbidden");

        var history = await _db.LoginHistories
            .OrderByDescending(h => h.AttemptedAt)
            .Take(500)
            .ToListAsync();
        return View(history);
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
