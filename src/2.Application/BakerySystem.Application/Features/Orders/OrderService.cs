using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Domain.Entities;
using BakerySystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.Application.Features.Orders;

public sealed class OrderService(IBakeryDbContext db)
{
    public Task<List<Order>> GetOrdersAsync(CancellationToken ct = default) =>
        GetOrdersAsync(null, ct);

    public Task<List<Order>> GetOrdersAsync(string[]? statuses, CancellationToken ct = default)
    {
        var query = db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.Category)
            .AsQueryable();

        if (statuses is { Length: > 0 })
            query = query.Where(o => statuses.Contains(o.Status));

        return query
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(ct);
    }

    public Task<List<Order>> GetRecentCompletedAsync(int count = 3, CancellationToken ct = default) =>
        db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed)
            .OrderByDescending(o => o.OrderDate)
            .Take(count)
            .ToListAsync(ct);

    public async Task<bool> ChangeStatusAsync(Order order, string newStatus, string? actorRole = null, CancellationToken ct = default)
    {
        if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed)
            return false;

        if (actorRole == AccountRole.Shipper)
        {
            var shipperTransitionAllowed =
                (order.Status == OrderStatus.Confirmed && newStatus == OrderStatus.Shipping) ||
                (order.Status == OrderStatus.Shipping && newStatus == OrderStatus.Delivered) ||
                (order.Status == OrderStatus.Shipping && newStatus == OrderStatus.Completed) ||
                (order.Status == OrderStatus.Delivered && newStatus == OrderStatus.Completed);

            if (!shipperTransitionAllowed)
                return false;
        }

        if (actorRole is AccountRole.Admin or AccountRole.Employee)
        {
            // Admin/Employee không được tự ý bấm hoàn tất khi đơn đang giao ngoài đường hoặc chờ khách nhận
            if ((order.Status == OrderStatus.Shipping || order.Status == OrderStatus.Delivered) && newStatus == OrderStatus.Completed)
                return false;
        }

        if (order.Status == OrderStatus.Pending && newStatus == OrderStatus.Confirmed)
        {
            foreach (var detail in order.OrderDetails)
            {
                var product = await db.Products.FirstOrDefaultAsync(p => p.ProductID == detail.ProductID, ct);
                if (product == null || product.Quantity < detail.Quantity)
                    return false;
            }

            foreach (var detail in order.OrderDetails)
            {
                var product = await db.Products.FirstAsync(p => p.ProductID == detail.ProductID, ct);
                product.Quantity -= detail.Quantity;
            }
        }

        order.Status = newStatus;
        if (newStatus == OrderStatus.Completed)
            order.PaymentStatus = PaymentStatus.Paid;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task CancelAsync(Order order, string reason, CancellationToken ct = default)
    {
        var shouldRestoreStock = order.Status is OrderStatus.Confirmed
            or OrderStatus.InProgress
            or OrderStatus.Shipping
            or OrderStatus.Delivered
            or OrderStatus.Completed;

        order.Status = OrderStatus.Cancelled;
        order.Note = string.IsNullOrWhiteSpace(order.Note)
            ? $"Lý do hủy: {reason}"
            : $"{order.Note}\nLý do hủy: {reason}";

        if (shouldRestoreStock)
        {
            foreach (var detail in order.OrderDetails)
            {
                var product = await db.Products.FindAsync([detail.ProductID], ct);
                if (product != null)
                    product.Quantity += detail.Quantity;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
