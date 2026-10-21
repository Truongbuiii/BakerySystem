using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.Application.Features.CustomerOrders;

public sealed class CustomerOrderService(IBakeryDbContext db)
{
    public Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default) =>
        db.Categories.ToListAsync(ct);

    public Task<List<Product>> GetActiveProductsAsync(CancellationToken ct = default) =>
        db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .ToListAsync(ct);

    public Task<List<Promotion>> GetActivePromotionsAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        return db.Promotions
            .Include(p => p.PromotionInvoice)
            .Where(p => p.StartDate <= today
                && p.EndDate >= today
                && (p.PromotionInvoice == null
                    || p.PromotionInvoice.UsageLimit == null
                    || p.PromotionInvoice.UsedCount < p.PromotionInvoice.UsageLimit))
            .OrderByDescending(p => p.PromotionInvoice != null
                ? p.PromotionInvoice.DiscountPercent
                : 0)
            .ToListAsync(ct);
    }

    public Task<List<Order>> GetOrdersAsync(
        int? customerId,
        string? lookupPhone,
        CancellationToken ct = default)
    {
        var query = db.Orders
            .AsNoTracking()
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerID == customerId.Value);
        else if (!string.IsNullOrWhiteSpace(lookupPhone))
            query = query.Where(o => o.ReceiverPhone != null
                && o.ReceiverPhone.Contains(lookupPhone.Trim()));

        return query
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(ct);
    }
}
