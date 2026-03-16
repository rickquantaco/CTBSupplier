using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CTBSupplier.Web.Controllers;

[AllowAnonymous]
public class AuthController : Controller
{
    // Shown when a user's Entra email is not found in the AppUser table
    public IActionResult NotRegistered() => View();

    // Shown when a user's Entra email is found but IsActive = false
    public IActionResult Inactive() => View();
}
