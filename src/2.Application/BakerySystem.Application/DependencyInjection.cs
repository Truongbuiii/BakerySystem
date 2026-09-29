using BakerySystem.Application.Features.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace BakerySystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
