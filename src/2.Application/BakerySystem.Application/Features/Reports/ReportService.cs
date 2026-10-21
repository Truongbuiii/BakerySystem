using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.Application.Features.Reports;

public sealed class ReportQueryResult
{
    public List<Order> Orders { get; init; } = [];
    public List<ImportReceipt> Imports { get; init; } = [];
    public List<OrderDetail> OrderDetails { get; init; } = [];
    public List<Product> LowStockProducts { get; init; } = [];
    public int SuppliersCount { get; init; }
}

public sealed class ReportService(IBakeryDbContext db)
{
    public async Task<ReportQueryResult> GetReportDataAsync(
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default)
    {
        var ordersQuery = db.Orders.AsQueryable();
        var importsQuery = db.ImportReceipts.AsQueryable();

        if (startDate.HasValue && endDate.HasValue)
        {
            ordersQuery = ordersQuery.Where(o =>
                o.OrderDate >= startDate.Value && o.OrderDate <= endDate.Value);
            importsQuery = importsQuery.Where(i =>
                i.ReceiptDate >= startDate.Value && i.ReceiptDate <= endDate.Value);
        }

        var orders = await ordersQuery.ToListAsync(ct);
        var imports = await importsQuery.ToListAsync(ct);
        var validOrderIds = orders
            .Where(o => o.Status != "Cancelled")
            .Select(o => o.OrderID)
            .ToList();

        var orderDetails = await db.OrderDetails
            .Include(d => d.Product)
                .ThenInclude(p => p.Category)
            .Where(d => validOrderIds.Contains(d.OrderID))
            .ToListAsync(ct);

        var lowStockProducts = await db.Products
            .Include(p => p.Category)
            .Where(p => p.Quantity <= 10 && p.IsActive)
            .OrderBy(p => p.Quantity)
            .ToListAsync(ct);

        return new ReportQueryResult
        {
            Orders = orders,
            Imports = imports,
            OrderDetails = orderDetails,
            LowStockProducts = lowStockProducts,
            SuppliersCount = await db.Suppliers.CountAsync(ct)
        };
    }
}
