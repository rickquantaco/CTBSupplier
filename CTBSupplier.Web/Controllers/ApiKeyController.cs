using System.Security.Cryptography;
using CTBSupplier.Web.Data;
using CTBSupplier.Web.Models;
using CTBSupplier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTBSupplier.Web.Controllers;

[Authorize]
public class ApiKeyController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly PermissionService _permissions;

    public ApiKeyController(ApplicationDbContext db, PermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    // GET: /ApiKey
    public async Task<IActionResult> Index()
    {
        if (!await CurrentUserIsAdminAsync())
            return View("Forbidden");

        var keys = await _db.ApiKeys
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
        return View(keys);
    }

    // GET: /ApiKey/Create
    public async Task<IActionResult> Create()
    {
        if (!await CurrentUserIsAdminAsync())
            return View("Forbidden");

        return View();
    }

    // POST: /ApiKey/Create — generates the key, shows it ONCE, stores only the hash
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string keyName)
    {
        if (!await CurrentUserIsAdminAsync())
            return View("Forbidden");

        if (string.IsNullOrWhiteSpace(keyName))
        {
            ModelState.AddModelError(nameof(keyName), "A name is required.");
            return View();
        }

        // Generate a cryptographically random key: "CTB_" + 32 random bytes as URL-safe base64
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var plaintext   = "CTB_" + Convert.ToBase64String(randomBytes)
                              .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var hash = ApiKeyAuthenticationHandler.ComputeSha256Hex(plaintext);

        _db.ApiKeys.Add(new ApiKey
        {
            KeyName   = keyName.Trim(),
            KeyHash   = hash,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Pass the plaintext to the view — shown ONCE, never stored
        ViewBag.NewKey  = plaintext;
        ViewBag.KeyName = keyName.Trim();
        return View("Created");
    }

    // POST: /ApiKey/Revoke/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(int id)
    {
        if (!await CurrentUserIsAdminAsync())
            return View("Forbidden");

        var key = await _db.ApiKeys.FindAsync(id);
        if (key != null)
        {
            key.IsActive = false;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /ApiKey/Enable/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable(int id)
    {
        if (!await CurrentUserIsAdminAsync())
            return View("Forbidden");

        var key = await _db.ApiKeys.FindAsync(id);
        if (key != null)
        {
            key.IsActive = true;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /ApiKey/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await CurrentUserIsAdminAsync())
            return View("Forbidden");

        var key = await _db.ApiKeys.FindAsync(id);
        if (key != null)
        {
            _db.ApiKeys.Remove(key);
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
