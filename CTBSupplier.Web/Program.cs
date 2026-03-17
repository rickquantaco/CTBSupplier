using Asp.Versioning;
using CTBSupplier.Web.Data;
using CTBSupplier.Web.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.OpenApi;
using System.Reflection;

namespace CTBSupplier.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // EF Core
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // LoginHistory and permission services
        builder.Services.AddScoped<LoginHistoryService>();
        builder.Services.AddScoped<PermissionService>();
        builder.Services.AddScoped<SupplierAccessService>();

        // Microsoft Entra (Azure AD) authentication with custom token validation
        builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(options =>
            {
                builder.Configuration.GetSection("AzureAd").Bind(options);

                // Use pure authorization code flow — avoids needing implicit grant enabled
                options.ResponseType = OpenIdConnectResponseType.Code;

                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = async context =>
                    {
                        // Extract email — Entra sends it as "preferred_username" or "email"
                        var email = context.Principal?.FindFirst("preferred_username")?.Value
                                 ?? context.Principal?.FindFirst("email")?.Value
                                 ?? context.Principal?.Identity?.Name
                                 ?? string.Empty;

                        var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();

                        var scope = context.HttpContext.RequestServices;
                        var db = scope.GetRequiredService<ApplicationDbContext>();
                        var loginSvc = scope.GetRequiredService<LoginHistoryService>();

                        var appUser = await db.AppUsers
                            .FirstOrDefaultAsync(u => u.UserEmail == email);

                        if (appUser == null)
                        {
                            // Email not in User table
                            await loginSvc.RecordAsync(email, isSuccess: false, ipAddress);
                            context.Response.Redirect("/Auth/NotRegistered");
                            context.HandleResponse();
                            return;
                        }

                        if (!appUser.IsActive)
                        {
                            // User exists but is inactive
                            await loginSvc.RecordAsync(email, isSuccess: false, ipAddress);
                            context.Response.Redirect("/Auth/Inactive");
                            context.HandleResponse();
                            return;
                        }

                        // Successful login
                        await loginSvc.RecordAsync(email, isSuccess: true, ipAddress);
                    }
                };
            });

        // API versioning — URL path style: /api/v1/suppliers
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion                = new ApiVersion(1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions               = true;  // adds api-supported-versions response header
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat           = "'v'VVV";  // e.g. "v1"
            options.SubstituteApiVersionInUrl = true;
        });

        // Swagger / OpenAPI
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "CTB Supplier API",
                Version     = "v1",
                Description = "Returns supplier details and stock-item counts. " +
                              "All requests must include a valid API key in the **X-API-Key** header. " +
                              "Keys are generated via Admin → API Keys."
            });

            // X-API-Key security definition
            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Type        = SecuritySchemeType.ApiKey,
                In          = ParameterLocation.Header,
                Name        = "X-API-Key",
                Description = "API key issued by the CTB Supplier admin. " +
                              "Example: `CTB_abc123...`"
            });

            // Apply the security requirement to every operation
            options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference("ApiKey", doc, null!), new List<string>() }
            });

            // Honour C# nullable reference type annotations:
            //   string  → not nullable, required in schema
            //   string? → nullable, optional in schema
            options.SupportNonNullableReferenceTypes();
            options.NonNullableReferenceTypesAsRequired();

            // Pull in XML comments from the controller (summary/remarks/param tags)
            var xmlPath = Path.Combine(AppContext.BaseDirectory,
                $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });

        // API Key authentication scheme (for REST API callers)
        builder.Services.AddAuthentication()
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyDefaults.AuthenticationScheme, _ => { });

        builder.Services.AddControllersWithViews()
            .AddMicrosoftIdentityUI()
            .AddJsonOptions(options =>
            {
                // API responses use camelCase property names
                options.JsonSerializerOptions.PropertyNamingPolicy =
                    System.Text.Json.JsonNamingPolicy.CamelCase;
            });

        builder.Services.AddRazorPages();

        // Require authentication on all controllers by default
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseRequestLocalization(new RequestLocalizationOptions()
            .SetDefaultCulture("en-AU")
            .AddSupportedCultures("en-AU")
            .AddSupportedUICultures("en-AU"));

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        // Swagger middleware runs before auth so the UI is accessible without sign-in
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "CTB Supplier API v1");
            options.RoutePrefix = "swagger";
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();           // attribute-routed API controllers
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Supplier}/{action=Index}/{id?}");
        app.MapRazorPages();

        app.Run();
    }
}
