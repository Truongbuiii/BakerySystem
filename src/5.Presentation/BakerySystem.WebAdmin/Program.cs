using BakerySystem.Application;
using BakerySystem.Infrastructure;
using BakerySystem.Infrastructure.Data;
using BakerySystem.WebAdmin.Components;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Đăng ký tầng Application và tầng Infrastructure (kết nối SQL Server qua Entity Framework Core)
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Khởi tạo và tự động Seed dữ liệu mẫu nếu database đang trống
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
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
