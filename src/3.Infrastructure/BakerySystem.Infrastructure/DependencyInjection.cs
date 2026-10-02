using System;
using System.Linq;
using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BakerySystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var configuredString = configuration.GetConnectionString("DefaultConnection");

        // Danh sách các instance SQL Server thường dùng trên laptop các thành viên:
        // - Máy bạn: (local) hoặc . (Default Instance)
        // - Máy bạn Thanh Tuyền: .\SQLEXPRESS hoặc (local)\SQLEXPRESS
        // - localhost
        string[] candidateServers = [
            ".\\SQLEXPRESS",
            "(local)\\SQLEXPRESS",
            "(local)",
            ".",
            "localhost"
        ];

        // Nếu người dùng có cấu hình cụ thể trong appsettings, đưa server đó lên ưu tiên trước
        if (!string.IsNullOrWhiteSpace(configuredString))
        {
            try
            {
                var connBuilder = new SqlConnectionStringBuilder(configuredString);
                if (!string.IsNullOrWhiteSpace(connBuilder.DataSource))
                {
                    candidateServers = [connBuilder.DataSource, .. candidateServers.Where(s => !string.Equals(s, connBuilder.DataSource, StringComparison.OrdinalIgnoreCase))];
                }
            }
            catch { }
        }

        string resolvedServer = candidateServers[0];

        foreach (var server in candidateServers)
        {
            // Kiểm tra kết nối tới Database=master (vì master luôn có sẵn trên mọi SQL Server, kể cả khi chưa tạo DB BakerySystem)
            var probeConnStr = $"Server={server};Database=master;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Connect Timeout=2;";
            try
            {
                using var conn = new SqlConnection(probeConnStr);
                conn.Open();
                resolvedServer = server;
                break;
            }
            catch
            {
                // Instance này không phản hồi hoặc không tồn tại trên máy hiện tại, thử instance tiếp theo
            }
        }

        string finalConnection = $"Server={resolvedServer};Database=BakerySystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

        services.AddDbContext<BakeryDbContext>(options =>
            options.UseSqlServer(finalConnection, b => b.MigrationsAssembly(typeof(BakeryDbContext).Assembly.FullName)));

        services.AddScoped<IBakeryDbContext>(provider => provider.GetRequiredService<BakeryDbContext>());

        return services;
    }
}
