using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BakerySystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var configuredString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=(local);Database=BakerySystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        // Tự động phát hiện instance SQL Server đang chạy (hỗ trợ cả máy cài (local), SQLEXPRESS hoặc localhost)
        string[] candidates = [
            configuredString,
            "Server=(local);Database=BakerySystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Connect Timeout=2;",
            "Server=.\\SQLEXPRESS;Database=BakerySystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Connect Timeout=2;",
            "Server=localhost;Database=BakerySystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Connect Timeout=2;"
        ];

        string resolvedConnection = configuredString;
        foreach (var candidate in candidates)
        {
            try
            {
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(candidate);
                conn.Open();
                resolvedConnection = candidate;
                break;
            }
            catch { }
        }

        services.AddDbContext<BakeryDbContext>(options =>
            options.UseSqlServer(resolvedConnection, b => b.MigrationsAssembly(typeof(BakeryDbContext).Assembly.FullName)));

        services.AddScoped<IBakeryDbContext>(provider => provider.GetRequiredService<BakeryDbContext>());

        return services;
    }
}
