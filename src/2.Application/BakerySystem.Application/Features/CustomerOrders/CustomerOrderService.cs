using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Domain.Entities;
using BakerySystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.Application.Features.CustomerOrders;

public sealed record CustomerOrderLineInput(int ProductId, int Quantity);

public sealed record CreateCustomerOrderInput(
    int CustomerId,
    string ReceiverName,
    string ReceiverPhone,
    string ShippingAddress,
    string? Note,
    string PaymentMethod,
    int? PromotionId,
    IReadOnlyCollection<CustomerOrderLineInput> Lines);

public sealed class CustomerOrderService(IBakeryDbContext db)
{
    public Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default) =>
        db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CategoryName)
            .ToListAsync(ct);

    public Task<List<Product>> GetActiveProductsAsync(CancellationToken ct = default) =>
        db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && (p.Category == null || p.Category.IsActive))
            .OrderBy(p => p.ProductName)
            .ToListAsync(ct);

    public Task<List<Promotion>> GetActivePromotionsAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        return db.Promotions
            .AsNoTracking()
            .Include(p => p.PromotionInvoice)
            .Include(p => p.PromotionProducts)
                .ThenInclude(pp => pp.Product)
            .Where(p => p.Status == PromotionStatus.Active
                && p.StartDate <= today
                && p.EndDate >= today
                && (p.PromotionInvoice != null || p.PromotionProducts.Any())
                && (p.PromotionInvoice == null
                    || p.PromotionInvoice.UsageLimit == null
                    || p.PromotionInvoice.UsedCount < p.PromotionInvoice.UsageLimit))
            .OrderByDescending(p => p.PromotionInvoice != null
                ? (p.PromotionInvoice.DiscountAmount ?? p.PromotionInvoice.DiscountPercent)
                : (p.PromotionProducts.FirstOrDefault() != null ? p.PromotionProducts.First().DiscountPercent : 0))
            .ToListAsync(ct);
    }

    public async Task<Promotion?> FindPromotionByCodeAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var normalized = code.Trim().ToUpperInvariant();
        var today = DateTime.Today;
        return await db.Promotions
            .AsNoTracking()
            .Include(p => p.PromotionInvoice)
            .Include(p => p.PromotionProducts)
                .ThenInclude(pp => pp.Product)
            .FirstOrDefaultAsync(p => p.PromotionCode.ToUpper() == normalized
                && p.Status == PromotionStatus.Active
                && p.StartDate <= today
                && p.EndDate >= today
                && (p.PromotionInvoice != null || p.PromotionProducts.Any())
                && (p.PromotionInvoice == null
                    || p.PromotionInvoice.UsageLimit == null
                    || p.PromotionInvoice.UsedCount < p.PromotionInvoice.UsageLimit), ct);
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

    public async Task<Order> CreateOrderAsync(CreateCustomerOrderInput input, CancellationToken ct = default)
    {
        if (input.Lines.Count == 0)
            throw new InvalidOperationException("Giỏ hàng đang trống. Vui lòng chọn ít nhất một món bánh.");

        if (string.IsNullOrWhiteSpace(input.ReceiverName))
            throw new InvalidOperationException("Vui lòng nhập họ tên người nhận bánh.");

        if (string.IsNullOrWhiteSpace(input.ReceiverPhone))
            throw new InvalidOperationException("Vui lòng nhập số điện thoại liên hệ.");

        if (string.IsNullOrWhiteSpace(input.ShippingAddress))
            throw new InvalidOperationException("Vui lòng nhập địa chỉ giao bánh.");

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerID == input.CustomerId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy hồ sơ khách hàng. Vui lòng đăng nhập lại.");

        var submittedLines = input.Lines
            .GroupBy(line => line.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(line => line.Quantity)
            })
            .ToList();

        if (submittedLines.Any(line => line.Quantity <= 0))
            throw new InvalidOperationException("Số lượng sản phẩm phải lớn hơn 0.");

        var productIds = submittedLines.Select(line => line.ProductId).ToArray();
        var products = await db.Products
            .Where(p => productIds.Contains(p.ProductID) && p.IsActive)
            .ToDictionaryAsync(p => p.ProductID, ct);

        decimal totalAmount = 0;
        var orderLines = new List<OrderDetail>();
        foreach (var line in submittedLines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                throw new InvalidOperationException("Một số món bánh trong giỏ đã ngừng kinh doanh.");

            if (product.Quantity < line.Quantity)
                throw new InvalidOperationException($"Món '{product.ProductName}' chỉ còn {product.Quantity} phần trong kho.");

            var subTotal = product.Price * line.Quantity;
            totalAmount += subTotal;
            orderLines.Add(new OrderDetail
            {
                ProductID = product.ProductID,
                Quantity = line.Quantity,
                UnitPrice = product.Price,
                DiscountAmount = 0,
                SubTotal = subTotal
            });
        }

        decimal discountAmount = 0;
        Promotion? promotion = null;
        if (input.PromotionId.HasValue)
        {
            var today = DateTime.Today;
            promotion = await db.Promotions
                .Include(p => p.PromotionInvoice)
                .Include(p => p.PromotionProducts)
                .FirstOrDefaultAsync(p => p.PromotionID == input.PromotionId.Value
                    && p.Status == PromotionStatus.Active
                    && p.StartDate <= today
                    && p.EndDate >= today, ct);

            var linkedPromotionOrderCount = promotion is null
                ? 0
                : await db.Orders.CountAsync(order => order.PromotionID == promotion.PromotionID, ct);

            if (promotion is null
                || (promotion.PromotionInvoice is { UsageLimit: not null } invoice && linkedPromotionOrderCount >= invoice.UsageLimit))
            {
                throw new InvalidOperationException("Mã khuyến mãi không còn hiệu lực hoặc đã hết lượt sử dụng.");
            }

            if (promotion.PromotionInvoice is { } invoiceCondition && totalAmount >= invoiceCondition.MinOrderAmount)
            {
                decimal invoiceDiscount;
                if (invoiceCondition.DiscountAmount is > 0)
                {
                    invoiceDiscount = invoiceCondition.DiscountAmount.Value;
                }
                else
                {
                    invoiceDiscount = totalAmount * invoiceCondition.DiscountPercent / 100m;
                    if (invoiceCondition.MaxDiscountAmount is > 0)
                        invoiceDiscount = Math.Min(invoiceDiscount, invoiceCondition.MaxDiscountAmount.Value);
                }
                discountAmount += invoiceDiscount;
            }

            foreach (var line in orderLines)
            {
                var productCondition = promotion.PromotionProducts
                    .FirstOrDefault(condition => condition.ProductID == line.ProductID);
                if (productCondition is null)
                    continue;

                var productDiscount = line.SubTotal * productCondition.DiscountPercent / 100m;
                if (productCondition.MaxDiscountAmount is > 0)
                    productDiscount = Math.Min(productDiscount, productCondition.MaxDiscountAmount.Value);
                var cappedLineDiscount = Math.Min(line.SubTotal, productDiscount);
                line.DiscountAmount = Math.Round(cappedLineDiscount, 0);
                discountAmount += cappedLineDiscount;
            }

            if (discountAmount <= 0)
                throw new InvalidOperationException("Đơn hàng chưa đủ điều kiện áp dụng mã khuyến mãi đã chọn.");
        }

        discountAmount = Math.Min(totalAmount, Math.Round(discountAmount, 0));

        var paymentMethod = string.IsNullOrWhiteSpace(input.PaymentMethod)
            ? PaymentMethod.COD
            : input.PaymentMethod.Trim();

        var order = new Order
        {
            CustomerID = customer.CustomerID,
            EmployeeID = null,
            OrderDate = DateTime.Now,
            TotalAmount = totalAmount,
            DiscountAmount = discountAmount,
            FinalAmount = totalAmount - discountAmount,
            ReceiverName = input.ReceiverName.Trim(),
            ReceiverPhone = input.ReceiverPhone.Trim(),
            ShippingAddress = input.ShippingAddress.Trim(),
            Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim(),
            PaymentMethod = paymentMethod,
            PaymentStatus = paymentMethod == PaymentMethod.COD
                ? PaymentStatus.Unpaid
                : PaymentStatus.Paid,
            Status = OrderStatus.Pending,
            PromotionID = promotion?.PromotionID
        };

        foreach (var orderLine in orderLines)
            order.OrderDetails.Add(orderLine);

        if (promotion?.PromotionInvoice is { } usedInvoice)
        {
            var linkedPromotionOrderCount = await db.Orders.CountAsync(o => o.PromotionID == promotion.PromotionID, ct);
            usedInvoice.UsedCount = linkedPromotionOrderCount + 1;
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        return await db.Orders
            .AsNoTracking()
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
            .FirstAsync(o => o.OrderID == order.OrderID, ct);
    }

    public async Task<bool> CancelOrderAsync(int orderId, int customerId, CancellationToken ct = default)
    {
        var order = await db.Orders.FirstOrDefaultAsync(
            o => o.OrderID == orderId && o.CustomerID == customerId, ct);
        if (order is null || order.Status != OrderStatus.Pending)
            return false;

        order.Status = OrderStatus.Cancelled;
        order.Note = string.IsNullOrWhiteSpace(order.Note)
            ? "Khách hàng tự hủy đơn trên Web"
            : $"{order.Note}\nKhách hàng tự hủy đơn trên Web";

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ConfirmReceivedOrderAsync(int orderId, int customerId, CancellationToken ct = default)
    {
        var order = await db.Orders.FirstOrDefaultAsync(
            o => o.OrderID == orderId && o.CustomerID == customerId, ct);
        if (order is null || (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Shipping))
            return false;

        order.Status = OrderStatus.Completed;
        order.PaymentStatus = PaymentStatus.Paid;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
