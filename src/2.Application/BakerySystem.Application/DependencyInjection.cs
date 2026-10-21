using BakerySystem.Application.Features.Auth;
using BakerySystem.Application.Features.Dashboard;
using BakerySystem.Application.Features.Products;
using BakerySystem.Application.Features.Inventory;
using BakerySystem.Application.Features.Employees;
using BakerySystem.Application.Features.Orders;
using BakerySystem.Application.Features.Pos;
using BakerySystem.Application.Features.CustomerOrders;
using BakerySystem.Application.Features.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace BakerySystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<PromotionService>();
        services.AddScoped<EmployeeService>();
        services.AddScoped<OrderService>();
        services.AddScoped<PosService>();
        services.AddScoped<CustomerOrderService>();
        services.AddScoped<ReportService>();
        return services;
    }
}
