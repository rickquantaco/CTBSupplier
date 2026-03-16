using CTBSupplier.Web.Data;
using CTBSupplier.Web.Models;

namespace CTBSupplier.Web.Services;

public class LoginHistoryService
{
    private readonly ApplicationDbContext _db;

    public LoginHistoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task RecordAsync(string loginName, bool isSuccess, string? ipAddress)
    {
        _db.LoginHistories.Add(new LoginHistory
        {
            LoginName   = loginName,
            AttemptedAt = DateTime.UtcNow,
            IsSuccess   = isSuccess,
            IpAddress   = ipAddress
        });
        await _db.SaveChangesAsync();
    }
}
