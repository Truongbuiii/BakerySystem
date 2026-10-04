using BakerySystem.Application.Features.Auth;
using BakerySystem.Application.Features.Dashboard;
using BakerySystem.Application.Features.Products;
using Microsoft.Extensions.DependencyInjection;

namespace BakerySystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IProductService, ProductService>();
        return services;
    }
}
