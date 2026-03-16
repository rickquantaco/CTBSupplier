using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using CTBSupplier.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CTBSupplier.Web.Services;

public static class ApiKeyDefaults
{
    public const string AuthenticationScheme = "ApiKey";
    public const string HeaderName           = "X-API-Key";
}

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions { }

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly ApplicationDbContext _db;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApplicationDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyDefaults.HeaderName, out var keyValues))
            return AuthenticateResult.NoResult();   // header absent — let other schemes try

        var providedKey = keyValues.ToString().Trim();
        if (string.IsNullOrEmpty(providedKey))
            return AuthenticateResult.Fail("Empty API key.");

        var hash = ComputeSha256Hex(providedKey);

        var apiKey = await _db.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == hash && k.IsActive);

        if (apiKey == null)
            return AuthenticateResult.Fail("Invalid or inactive API key.");

        var claims = new[]
        {
            new Claim(ClaimTypes.Name,           apiKey.KeyName),
            new Claim(ClaimTypes.NameIdentifier, apiKey.ApiKeyId.ToString()),
            new Claim("auth_scheme",             ApiKeyDefaults.AuthenticationScheme)
        };

        var identity  = new ClaimsIdentity(claims, ApiKeyDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, ApiKeyDefaults.AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode  = 401;
        Response.ContentType = "application/json";
        return Response.WriteAsync(
            $"{{\"error\":\"Unauthorised. Supply a valid {ApiKeyDefaults.HeaderName} header.\"}}");
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode  = 403;
        Response.ContentType = "application/json";
        return Response.WriteAsync("{\"error\":\"Forbidden.\"}");
    }

    public static string ComputeSha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
