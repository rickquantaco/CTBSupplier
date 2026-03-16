using CTBSupplier.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace CTBSupplier.Web.Services;

public class PermissionService
{
    private readonly ApplicationDbContext _db;

    public PermissionService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns true if the user identified by <paramref name="email"/> holds
    /// the permission whose Description matches <paramref name="permissionName"/>.
    /// </summary>
    public async Task<bool> UserHasPermissionAsync(string email, string permissionName)
    {
        return await _db.AppUserPermissions
            .AnyAsync(up =>
                up.AppUser.UserEmail.ToLower() == email.ToLower() &&
                up.AppUser.IsActive &&
                up.AppPermission.Description == permissionName);
    }
}
