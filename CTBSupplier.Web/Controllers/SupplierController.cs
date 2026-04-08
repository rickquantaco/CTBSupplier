using CTBSupplier.Web.Data;
using CTBSupplier.Web.Models;
using CTBSupplier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTBSupplier.Web.Controllers;

[Authorize]
public class SupplierController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly SupplierAccessService _access;
    private readonly IHttpClientFactory _httpClientFactory;

    public SupplierController(ApplicationDbContext db, SupplierAccessService access, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _access = access;
        _httpClientFactory = httpClientFactory;
    }

    // GET: /Supplier
    public async Task<IActionResult> Index()
    {
        var allowed = await _access.GetAllowedSupplierGuidsAsync(User);

        IQueryable<Supplier> query = _db.Suppliers.OrderBy(s => s.SupplierName);

        if (allowed != null)
            query = query.Where(s => allowed.Contains(s.SupplierGUID));

        ViewBag.IsRestricted = allowed != null;

        return View(await query.ToListAsync());
    }

    // GET: /Supplier/Details/{id}
    public async Task<IActionResult> Details(
        Guid id, int page = 1, int pageSize = 25,
        string? filterBrand = null, string? filterCategory = null,
        string? filterDescription = null,
        string? viewMode = null)
    {
        if (!await _access.CanAccessSupplierAsync(User, id))
            return View("Forbidden");

        if (pageSize != 25 && pageSize != 50 && pageSize != 100) pageSize = 25;
        if (page < 1) page = 1;

        // Resolve viewMode: explicit param → cookie → default "list"
        if (string.IsNullOrEmpty(viewMode))
            viewMode = Request.Cookies["SupplierStockViewMode"] ?? "list";
        if (viewMode != "grid") viewMode = "list";
        Response.Cookies.Append("SupplierStockViewMode", viewMode,
            new Microsoft.AspNetCore.Http.CookieOptions { MaxAge = TimeSpan.FromDays(365), IsEssential = true });

        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.SupplierGUID == id);
        if (supplier == null) return NotFound();

        // Load distinct brands and categories for this supplier (for the filter dropdowns)
        var allItems = _db.StockItems.Where(i => i.SupplierGUID == id);

        var hasNoBrand = await allItems.AnyAsync(i => i.BrandName == null || i.BrandName == "");

        var availableBrands = (await allItems
            .Where(i => i.BrandName != null && i.BrandName != "")
            .Select(i => i.BrandName!)
            .Distinct()
            .ToListAsync())
            .OrderBy(b => b, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (hasNoBrand)
            availableBrands.Insert(0, "(No Brand)");

        var availableCategories = (await allItems
            .Select(i => i.StockCategoryName)
            .Distinct()
            .ToListAsync())
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Apply filters then page
        var query = allItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filterBrand))
        {
            if (filterBrand == "(No Brand)")
                query = query.Where(i => i.BrandName == null || i.BrandName == "");
            else
                query = query.Where(i => i.BrandName == filterBrand);
        }

        if (!string.IsNullOrWhiteSpace(filterCategory))
            query = query.Where(i => i.StockCategoryName == filterCategory);

        if (!string.IsNullOrWhiteSpace(filterDescription))
            query = query.Where(i => i.StockDesc.Contains(filterDescription));

        query = query.OrderBy(i => i.StockCode);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new SupplierDetailsViewModel
        {
            Supplier            = supplier,
            StockItems          = items,
            Page                = page,
            PageSize            = pageSize,
            TotalCount          = total,
            FilterBrand         = filterBrand,
            FilterCategory      = filterCategory,
            FilterDescription   = filterDescription,
            AvailableBrands     = availableBrands,
            AvailableCategories = availableCategories,
            ViewMode            = viewMode
        };

        return View(vm);
    }

    // GET: /Supplier/ValidateStockItemURLs/{id}
    public async Task<IActionResult> ValidateStockItemURLs(Guid id)
    {
        if (!await _access.CanAccessSupplierAsync(User, id))
            return View("Forbidden");

        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.SupplierGUID == id);
        if (supplier == null) return NotFound();

        var allItems = await _db.StockItems
            .Where(i => i.SupplierGUID == id)
            .OrderBy(i => i.StockCode)
            .ToListAsync();

        var withUrl    = allItems.Where(i => !string.IsNullOrWhiteSpace(i.StockMediaUrl)).ToList();
        var withoutUrl = allItems.Count - withUrl.Count;

        var client    = _httpClientFactory.CreateClient("UrlValidator");
        var semaphore = new SemaphoreSlim(10);

        var tasks = withUrl.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                var result = new UrlCheckResult
                {
                    StockCode    = item.StockCode,
                    SupplierGUID = item.SupplierGUID,
                    StockDesc    = item.StockDesc,
                    Url          = item.StockMediaUrl!
                };

                try
                {
                    if (!await IsSafeExternalUrlAsync(item.StockMediaUrl))
                    {
                        result.ErrorMessage = "Blocked: URL must use http/https and must not resolve to a private or internal address.";
                    }
                    else
                    {
                        var request  = new HttpRequestMessage(HttpMethod.Head, item.StockMediaUrl);
                        var response = await client.SendAsync(request);
                        result.StatusCode = (int)response.StatusCode;
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = ex.Message;
                }

                return result;
            }
            finally
            {
                semaphore.Release();
            }
        });

        var allResults    = await Task.WhenAll(tasks);
        var failedResults = allResults
            .Where(r => !r.IsSuccess)
            .OrderBy(r => r.StockCode)
            .ToList();

        var vm = new ValidateStockItemUrlsViewModel
        {
            Supplier       = supplier,
            FailedResults  = failedResults,
            TotalWithUrl   = withUrl.Count,
            TotalWithoutUrl = withoutUrl
        };

        return View(vm);
    }

    // GET: /Supplier/Create
    public async Task<IActionResult> Create()
    {
        var allowed = await _access.GetAllowedSupplierGuidsAsync(User);
        if (allowed != null)
            return View("Forbidden");

        return View(new Supplier());
    }

    // POST: /Supplier/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Supplier supplier)
    {
        if (!ModelState.IsValid) return View(supplier);
        supplier.SupplierGUID = Guid.NewGuid();
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Supplier/Edit/{id}
    public async Task<IActionResult> Edit(Guid id)
    {
        if (!await _access.CanAccessSupplierAsync(User, id))
            return View("Forbidden");

        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();
        return View(supplier);
    }

    // POST: /Supplier/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Supplier supplier)
    {
        if (id != supplier.SupplierGUID) return BadRequest();
        if (!await _access.CanAccessSupplierAsync(User, id))
            return View("Forbidden");
        if (!ModelState.IsValid) return View(supplier);

        _db.Update(supplier);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Supplier/Delete/{id}
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await _access.CanAccessSupplierAsync(User, id))
            return View("Forbidden");

        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();
        return View(supplier);
    }

    // POST: /Supplier/Delete/{id}
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        if (!await _access.CanAccessSupplierAsync(User, id))
            return View("Forbidden");

        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier != null)
        {
            _db.Suppliers.Remove(supplier);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // Returns true only if the URL is http/https and resolves to a public (non-private) IP address.
    private static async Task<bool> IsSafeExternalUrlAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;

        try
        {
            var addresses = await System.Net.Dns.GetHostAddressesAsync(uri.Host);
            return addresses.Length > 0 && !addresses.Any(IsPrivateIpAddress);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPrivateIpAddress(System.Net.IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return b[0] == 127                                       // 127.0.0.0/8   loopback
                || b[0] == 10                                        // 10.0.0.0/8    private
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)        // 172.16.0.0/12 private
                || (b[0] == 192 && b[1] == 168)                     // 192.168.0.0/16 private
                || (b[0] == 169 && b[1] == 254)                     // 169.254.0.0/16 link-local / IMDS
                || b[0] == 0                                         // 0.0.0.0/8
                || (b[0] == 100 && b[1] >= 64 && b[1] <= 127);      // 100.64.0.0/10 shared address space
        }

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            if (ip.Equals(System.Net.IPAddress.IPv6Loopback)) return true;  // ::1
            var b = ip.GetAddressBytes();
            if ((b[0] & 0xFE) == 0xFC) return true;                         // fc00::/7  ULA
            if (b[0] == 0xFE && (b[1] & 0xC0) == 0x80) return true;        // fe80::/10 link-local
        }

        return false;
    }
}
