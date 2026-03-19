using System.Security.Claims;
using CTBSupplier.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace CTBSupplier.Web.Services;

/// <summary>
/// Resolves which suppliers the current user may access.
/// Returns null = unrestricted (sees all). Returns a HashSet = restricted to those suppliers only.
/// </summary>
public class SupplierAccessService
{
    private readonly ApplicationDbContext _db;

    public SupplierAccessService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the set of supplier GUIDs the user is locked to, or null if unrestricted.
    /// An empty set means the user has restrictions configured but none assigned (sees nothing).
    /// </summary>
    public async Task<HashSet<Guid>?> GetAllowedSupplierGuidsAsync(ClaimsPrincipal user)
    {
        var email = user.FindFirst("preferred_username")?.Value
                 ?? user.FindFirst("email")?.Value
                 ?? user.Identity?.Name
                 ?? string.Empty;

        if (string.IsNullOrEmpty(email))
            return null;

        var appUser = await _db.AppUsers
            .AsNoTracking()
            .Include(u => u.AppUserSuppliers)
            .FirstOrDefaultAsync(u => u.UserEmail == email);

        if (appUser == null)
            return null;

        // If the user has no supplier restrictions recorded, they are unrestricted
        if (appUser.AppUserSuppliers.Count == 0)
            return null;

        return appUser.AppUserSuppliers.Select(s => s.SupplierGUID).ToHashSet();
    }

    /// <summary>Returns true if the user may access the given supplier.</summary>
    public async Task<bool> CanAccessSupplierAsync(ClaimsPrincipal user, Guid supplierGuid)
    {
        var allowed = await GetAllowedSupplierGuidsAsync(user);
        return allowed == null || allowed.Contains(supplierGuid);
    }
}
