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

		// Kết nối trực tiếp cơ sở dữ liệu BakerySystem từ SQL Server
		const string connectionString = "Server=.\\SQLEXPRESS;Database=BakerySystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
		builder.Services.AddDbContext<BakeryDbContext>(options =>
			options.UseSqlServer(connectionString));

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
