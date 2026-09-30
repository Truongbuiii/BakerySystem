using BakerySystem.Application;
using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Infrastructure.Data;
using BakerySystem.MobileApp.Services;
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

		// Tự động phát hiện instance SQL Server đang chạy (hỗ trợ cả máy cài (local), SQLEXPRESS hoặc localhost)
		string[] candidateConnections = [
			"Server=(local);Database=BakerySystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Connect Timeout=2;",
			"Server=.\\SQLEXPRESS;Database=BakerySystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Connect Timeout=2;",
			"Server=localhost;Database=BakerySystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Connect Timeout=2;"
		];

		string resolvedConnection = candidateConnections[0];
		foreach (var candidate in candidateConnections)
		{
			try
			{
				using var testConn = new Microsoft.Data.SqlClient.SqlConnection(candidate);
				testConn.Open();
				resolvedConnection = candidate;
				break;
			}
			catch { }
		}

		builder.Services.AddDbContext<BakeryDbContext>(options =>
			options.UseSqlServer(resolvedConnection));

		builder.Services.AddScoped<IBakeryDbContext>(sp => sp.GetRequiredService<BakeryDbContext>());
		builder.Services.AddScoped<CartService>();
		builder.Services.AddApplicationServices();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
