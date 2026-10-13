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
            // Máy mới tinh chưa có DB/bảng: EF Core tự động tạo Database và các bảng
            await context.Database.MigrateAsync();
        }

        // Tự động đảm bảo bảng PromotionOrderTier và các cột mới cho 3 loại khuyến mãi tồn tại trong DB
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"
                IF OBJECT_ID(N'[PromotionOrderTier]') IS NULL
                BEGIN
                    CREATE TABLE [PromotionOrderTier] (
                        [PromotionID] int NOT NULL,
                        [MinOrderAmount] decimal(18,2) NOT NULL,
                        [DiscountPercent] decimal(5,2) NOT NULL DEFAULT 0,
                        [DiscountAmount] decimal(18,2) NULL,
                        [MaxDiscountAmount] decimal(18,2) NULL,
                        CONSTRAINT [PK_PromotionOrderTier] PRIMARY KEY ([PromotionID]),
                        CONSTRAINT [FK_PromotionOrderTier_Promotion_PromotionID] FOREIGN KEY ([PromotionID]) REFERENCES [Promotion] ([PromotionID]) ON DELETE CASCADE
                    );
                END;

                IF COL_LENGTH('Promotion', 'PromotionType') IS NULL
                BEGIN
                    ALTER TABLE [Promotion] ADD [PromotionType] varchar(30) NOT NULL DEFAULT 'Invoice';
                END;

                IF COL_LENGTH('PromotionInvoice', 'VoucherCode') IS NULL
                BEGIN
                    ALTER TABLE [PromotionInvoice] ADD [VoucherCode] varchar(50) NULL;
                END;

                IF COL_LENGTH('PromotionInvoice', 'DiscountAmount') IS NULL
                BEGIN
                    ALTER TABLE [PromotionInvoice] ADD [DiscountAmount] decimal(18,2) NULL;
                END;

                IF COL_LENGTH('PromotionInvoice', 'UsageLimit') IS NULL
                BEGIN
                    ALTER TABLE [PromotionInvoice] ADD [UsageLimit] int NULL;
                END;

                IF COL_LENGTH('PromotionInvoice', 'UsedCount') IS NULL
                BEGIN
                    ALTER TABLE [PromotionInvoice] ADD [UsedCount] int NOT NULL DEFAULT 0;
                END;

                IF COL_LENGTH('PromotionProduct', 'DiscountAmount') IS NULL
                BEGIN
                    ALTER TABLE [PromotionProduct] ADD [DiscountAmount] decimal(18,2) NULL;
                END;

                IF COL_LENGTH('Customer', 'LastName') IS NULL
                BEGIN
                    ALTER TABLE [Customer] ADD [LastName] nvarchar(100) NOT NULL DEFAULT N'';
                END;

                IF COL_LENGTH('Customer', 'FirstName') IS NULL
                BEGIN
                    ALTER TABLE [Customer] ADD [FirstName] nvarchar(50) NOT NULL DEFAULT N'';
                END;

                IF COL_LENGTH('Customer', 'FullName') IS NOT NULL
                BEGIN
                    ALTER TABLE [Customer] ALTER COLUMN [FullName] nvarchar(100) NULL;
                    EXEC sp_executesql N'
                        UPDATE [Customer] 
                        SET [FirstName] = CASE 
                                WHEN CHARINDEX('' '', REVERSE(LTRIM(RTRIM([FullName])))) > 0 
                                THEN REVERSE(SUBSTRING(REVERSE(LTRIM(RTRIM([FullName]))), 1, CHARINDEX('' '', REVERSE(LTRIM(RTRIM([FullName])))) - 1))
                                ELSE LTRIM(RTRIM([FullName]))
                            END,
                            [LastName] = CASE 
                                WHEN CHARINDEX('' '', REVERSE(LTRIM(RTRIM([FullName])))) > 0 
                                THEN LTRIM(RTRIM(SUBSTRING([FullName], 1, LEN([FullName]) - CHARINDEX('' '', REVERSE(LTRIM(RTRIM([FullName])))))))
                                ELSE N''''
                            END
                        WHERE ([FirstName] IS NULL OR [FirstName] = N'''') AND [FullName] IS NOT NULL AND LTRIM(RTRIM([FullName])) <> N'''';
                    ';
                END;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Orders_PromotionInvoice')
                BEGIN
                    ALTER TABLE [Orders] DROP CONSTRAINT [FK_Orders_PromotionInvoice];
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Orders_Promotion')
                BEGIN
                    ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_Promotion] FOREIGN KEY ([PromotionID]) REFERENCES [Promotion] ([PromotionID]) ON DELETE SET NULL;
                END;
            ");
        }
        catch { }

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
