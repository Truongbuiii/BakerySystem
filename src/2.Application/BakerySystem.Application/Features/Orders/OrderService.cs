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

    public async Task<bool> ChangeStatusAsync(Order order, string newStatus, string? actorRole = null, int? actorEmployeeId = null, CancellationToken ct = default)
    {
        if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed)
            return false;

        // Luồng đơn hàng rút gọn cho Admin / Nhân viên:
        // Pending (Chờ duyệt) -> Confirmed (Đã duyệt) -> Delivered (Giao hàng / Chờ khách xác nhận) -> Khách bấm Đã nhận -> Completed (Hoàn tất)
        var allowedTransition =
            (order.Status == OrderStatus.Pending && newStatus == OrderStatus.Confirmed) ||
            (order.Status == OrderStatus.Confirmed && newStatus == OrderStatus.Delivered) ||
            (order.Status == OrderStatus.Shipping && newStatus == OrderStatus.Delivered);

        if (!allowedTransition)
            return false;

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

        if (actorEmployeeId.HasValue && order.EmployeeID == null)
        {
            order.EmployeeID = actorEmployeeId.Value;
        }

        order.Status = newStatus;
        if (newStatus == OrderStatus.Completed)
            order.PaymentStatus = PaymentStatus.Paid;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> CancelAsync(Order order, string reason, CancellationToken ct = default)
    {
        // Khóa không cho phép hủy khi đơn đã được duyệt hoặc đang trong quá trình giao/hoàn tất.
        // Chỉ cho phép hủy/từ chối khi đơn đang ở trạng thái Pending (Chờ duyệt).
        if (order.Status != OrderStatus.Pending)
            return false;

        order.Status = OrderStatus.Cancelled;
        order.Note = string.IsNullOrWhiteSpace(order.Note)
            ? $"Lý do từ chối/hủy: {reason}"
            : $"{order.Note}\nLý do từ chối/hủy: {reason}";

        await db.SaveChangesAsync(ct);
        return true;
    }
}
