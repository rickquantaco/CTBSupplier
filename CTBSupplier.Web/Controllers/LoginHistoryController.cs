using CTBSupplier.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTBSupplier.Web.Controllers;

[Authorize]
public class LoginHistoryController : Controller
{
    private readonly ApplicationDbContext _db;

    public LoginHistoryController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /LoginHistory
    public async Task<IActionResult> Index()
    {
        var history = await _db.LoginHistories
            .OrderByDescending(h => h.AttemptedAt)
            .Take(500)
            .ToListAsync();
        return View(history);
    }
}
