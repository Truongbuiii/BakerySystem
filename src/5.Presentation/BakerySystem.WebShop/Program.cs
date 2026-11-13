using System.Security.Claims;
using BakerySystem.Application;
using BakerySystem.Application.Features.Auth;
using BakerySystem.Domain.Enums;
using BakerySystem.Infrastructure;
using BakerySystem.Infrastructure.Data;
using BakerySystem.WebShop.Components;
using BakerySystem.WebShop.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Đăng ký tầng Application và tầng Infrastructure (dùng chung SQL Server với WebAdmin)
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<WebCartService>();

// Cookie riêng biệt hoàn toàn với WebAdmin (.BakerySystem.ShopAuth)
// Không lưu cookie bền vững qua lần đóng trình duyệt, hết hạn sau 60 phút không hoạt động (tối đa 4 giờ)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".BakerySystem.ShopAuth";
        options.LoginPath = "/login";
        options.LogoutPath = "/api/shop/auth/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                var isValid = await SessionClaimHelper.ValidatePrincipalAsync(
                    context.Principal,
                    authService,
                    RevalidatingAuthStateProvider.MaxSessionLifetime,
                    context.HttpContext.RequestAborted);

                if (!isValid)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingAuthStateProvider>();

var app = builder.Build();

// Đồng bộ schema và dữ liệu mẫu nếu WebShop khởi động trước WebAdmin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BakeryDbContext>();
        bool tableExists = false;
        try
        {
            var conn = context.Database.GetDbConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Account'";
            var result = await cmd.ExecuteScalarAsync();
            tableExists = Convert.ToInt32(result) > 0;
            await conn.CloseAsync();
        }
        catch
        {
            tableExists = false;
        }

        if (!tableExists)
        {
            await context.Database.MigrateAsync();
        }

        await BakeryDataSeeder.EnsureDatabaseSchemaAsync(context);
        await BakeryDataSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khởi tạo cơ sở dữ liệu cho BakerySystem.WebShop.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Dùng chung thư mục uploads từ WebAdmin nếu tồn tại để ảnh sản phẩm Admin mới tải lên hiển thị ngay ở WebShop
var sharedUploadsDir = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "BakerySystem.WebAdmin", "wwwroot", "uploads"));
if (Directory.Exists(sharedUploadsDir))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(sharedUploadsDir),
        RequestPath = "/uploads"
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

// === Minimal API: Auth dành riêng cho Khách hàng (Customer) ===
app.MapPost("/api/shop/auth/login", async (LoginRequest request, [FromServices] IAuthService authService, HttpContext httpContext) =>
{
    var result = await authService.LoginAsync(request);
    if (!result.Success)
        return Results.Json(result);

    if (result.Role != AccountRole.Customer)
    {
        return Results.Json(new LoginResult
        {
            Success = false,
            ErrorMessage = "Tài khoản quản trị / nhân viên vui lòng đăng nhập tại cổng WebAdmin (localhost:5149)."
        });
    }

    await SignInCustomerCookieAsync(httpContext, authService, result.AccountId, result.Username, result.FullName);
    return Results.Json(result);
}).DisableAntiforgery();

app.MapPost("/api/shop/auth/register", async (CustomerRegisterRequest request, [FromServices] IAuthService authService, HttpContext httpContext) =>
{
    var result = await authService.RegisterCustomerAsync(request);
    if (!result.Success || result.Customer is null || result.Customer.AccountId is null)
        return Results.Json(new { success = false, errorMessage = result.ErrorMessage ?? "Đăng ký không thành công." });

    await SignInCustomerCookieAsync(httpContext, authService, result.Customer.AccountId.Value, result.Customer.Username, result.Customer.FullName);

    return Results.Json(new
    {
        success = true,
        role = AccountRole.Customer,
        username = result.Customer.Username,
        fullName = result.Customer.FullName
    });
}).DisableAntiforgery();

app.MapPost("/api/shop/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
}).DisableAntiforgery();

app.MapGet("/api/shop/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static async Task SignInCustomerCookieAsync(HttpContext httpContext, IAuthService authService, int accountId, string username, string? fullName)
{
    var sessionStamp = await authService.GetSessionStampAsync(accountId);
    var claims = SessionClaimHelper.BuildSessionClaims(
        accountId,
        username,
        AccountRole.Customer,
        fullName,
        sessionStamp);

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity),
        new AuthenticationProperties
        {
            IsPersistent = false
        });
}
