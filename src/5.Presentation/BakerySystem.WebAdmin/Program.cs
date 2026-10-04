using System.Security.Claims;
using BakerySystem.Application;
using BakerySystem.Application.Features.Auth;
using BakerySystem.Infrastructure;
using BakerySystem.Infrastructure.Data;
using BakerySystem.WebAdmin.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Đăng ký tầng Application và tầng Infrastructure (kết nối SQL Server qua Entity Framework Core)
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// === Authentication: Cookie-based ===
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/api/auth/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Tự động Migrate database và Seed dữ liệu mẫu nếu database chưa có hoặc đang trống
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BakeryDbContext>();
        
        // Kiểm tra xem bảng dữ liệu đã tồn tại sẵn (từ file script SQL) hay chưa
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

        if (tableExists)
        {
            // Database đã được tạo từ script SQL trước đó: đồng bộ lịch sử migration để EF Core không tạo đè
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
                    BEGIN
                        CREATE TABLE [__EFMigrationsHistory] (
                            [MigrationId] nvarchar(150) NOT NULL,
                            [ProductVersion] nvarchar(32) NOT NULL,
                            CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                        );
                    END;
                    IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260929091331_InitialCreate')
                    BEGIN
                        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                        VALUES ('20260929091331_InitialCreate', '10.0.0');
                    END;
                ");
            }
            catch { }
        }
        else
        {
            // Máy mới tinh chưa có DB/bảng: EF Core tự động tạo Database và 13 bảng
            await context.Database.MigrateAsync();
        }

        // Tự động nạp dữ liệu mẫu khởi tạo
        await BakeryDataSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi xảy ra trong quá trình khởi tạo dữ liệu mẫu cho BakeryDbContext.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

// === Minimal API: Auth endpoints ===
app.MapPost("/api/auth/login", async (LoginRequest request, [FromServices] IAuthService authService, HttpContext httpContext) =>
{
    var result = await authService.LoginAsync(request);
    if (!result.Success)
        return Results.Json(result);

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, result.AccountId.ToString()),
        new(ClaimTypes.Name, result.Username),
        new(ClaimTypes.Role, result.Role),
        new("FullName", result.FullName ?? result.Username)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity),
        new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

    return Results.Json(result);
}).DisableAntiforgery();

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
}).DisableAntiforgery();

app.MapGet("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
