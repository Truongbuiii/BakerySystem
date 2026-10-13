using System;
using System.Linq;
using BakerySystem.Application;
using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Infrastructure.Data;
using BakerySystem.MobileApp.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BakerySystem.MobileApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

		// Giữ cùng thứ tự ưu tiên với WebAdmin/Infrastructure để hai ứng dụng
		// không vô tình đọc hai database BakerySystem trên các SQL instance khác nhau.
		string[] candidateServers = [
			"(local)",
			".\\SQLEXPRESS",
			"(local)\\SQLEXPRESS",
			".",
			"localhost"
		];

		string resolvedServer = candidateServers[0];
		foreach (var server in candidateServers)
		{
			// Kiểm tra kết nối tới Database=master
			var probeConnStr = $"Server={server};Database=master;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Connect Timeout=2;";
			try
			{
				using var testConn = new SqlConnection(probeConnStr);
				testConn.Open();
				resolvedServer = server;
				break;
			}
			catch { }
		}

		string finalConnection = $"Server={resolvedServer};Database=BakerySystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

		builder.Services.AddDbContext<BakeryDbContext>(options =>
			options.UseSqlServer(finalConnection));

		builder.Services.AddScoped<IBakeryDbContext>(sp => sp.GetRequiredService<BakeryDbContext>());
		builder.Services.AddScoped<CartService>();
		builder.Services.AddScoped(_ => new HttpClient());
		builder.Services.AddApplicationServices();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		// Tự động kiểm tra và khởi tạo Database + Dữ liệu mẫu nếu chưa có
		_ = Task.Run(async () =>
		{
			try
			{
				using var scope = app.Services.CreateScope();
				var context = scope.ServiceProvider.GetRequiredService<BakeryDbContext>();
				await BakeryDataSeeder.EnsureDatabaseSchemaAsync(context);
				await context.Database.MigrateAsync();
				await BakeryDataSeeder.SeedAsync(context);
			}
			catch { }
		});

		return app;
	}
}
