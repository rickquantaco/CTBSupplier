using System.Security.Claims;
using CTBSupplier.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace CTBSupplier.Web.Services;

/// <summary>
/// Resolves whether the current user is restricted to a single supplier.
/// Returns null = unrestricted (sees all). Returns a Guid = restricted to that supplier only.
/// </summary>
public class SupplierAccessService
{
    private readonly ApplicationDbContext _db;

    public SupplierAccessService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Returns the supplier GUID the user is locked to, or null if unrestricted.</summary>
    public async Task<Guid?> GetRestrictedSupplierGuidAsync(ClaimsPrincipal user)
    {
        var email = user.FindFirst("preferred_username")?.Value
                 ?? user.FindFirst("email")?.Value
                 ?? user.Identity?.Name
                 ?? string.Empty;

        if (string.IsNullOrEmpty(email))
            return null;

        var appUser = await _db.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserEmail == email);

        return appUser?.SupplierGUID;
    }

    /// <summary>Returns true if the user may access the given supplier.</summary>
    public async Task<bool> CanAccessSupplierAsync(ClaimsPrincipal user, Guid supplierGuid)
    {
        var restriction = await GetRestrictedSupplierGuidAsync(user);
        return restriction == null || restriction.Value == supplierGuid;
    }
}
