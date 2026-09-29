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
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost;Database=BakerySystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<BakeryDbContext>(options =>
            options.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(BakeryDbContext).Assembly.FullName)));

        services.AddScoped<IBakeryDbContext>(provider => provider.GetRequiredService<BakeryDbContext>());

        return services;
    }
}
