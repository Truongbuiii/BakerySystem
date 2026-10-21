using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Domain.Entities;
using BakerySystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.Application.Features.Pos;

public sealed class PosCatalog
{
    public List<Category> Categories { get; init; } = [];
    public List<Product> Products { get; init; } = [];
    public List<Promotion> Promotions { get; init; } = [];
}

public sealed class PosOrderLine
{
    public int ProductID { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal SubTotal { get; init; }
}

public sealed class PosService(IBakeryDbContext db)
{
    public async Task<PosCatalog> GetCatalogAsync(CancellationToken ct = default)
    {
        var categories = await db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.CategoryName)
            .ToListAsync(ct);

        var products = await db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && (p.Category == null || p.Category.IsActive))
            .OrderBy(p => p.ProductName)
            .ToListAsync(ct);

        var today = DateTime.Today;
        var promotions = await db.Promotions
            .Include(p => p.PromotionInvoice)
            .Where(p => p.Status == PromotionStatus.Active
                     && p.PromotionInvoice != null
                     && p.StartDate <= today
                     && p.EndDate >= today
                     && (p.PromotionInvoice.UsageLimit == null
                         || p.PromotionInvoice.UsedCount < p.PromotionInvoice.UsageLimit))
            .OrderByDescending(p => p.PromotionInvoice!.DiscountPercent)
            .ToListAsync(ct);

        return new PosCatalog
        {
            Categories = categories,
            Products = products,
            Promotions = promotions
        };
    }

    public Task<Customer?> FindCustomerByPhoneAsync(string phone, CancellationToken ct = default)
    {
        var normalizedPhone = phone.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
        return db.Customers
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.Phone != null
                && c.Phone.Replace(" ", "").Replace(".", "").Replace("-", "") == normalizedPhone, ct);
    }

    public async Task<Customer> CreateCustomerAsync(
        string lastName,
        string firstName,
        string phone,
        CancellationToken ct = default)
    {
        var customer = new Customer
        {
            LastName = lastName.Trim(),
            FirstName = string.IsNullOrWhiteSpace(firstName) ? "Khách tại quầy" : firstName.Trim(),
            Phone = phone.Trim(),
            Address = "Mua trực tiếp tại tiệm bánh",
            AccountID = null
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return customer;
    }

    public Task<Promotion?> FindPromotionAsync(string code, CancellationToken ct = default) =>
        db.Promotions
            .Include(p => p.PromotionInvoice)
            .FirstOrDefaultAsync(p => p.PromotionCode.ToUpper() == code.Trim().ToUpper()
                && p.Status == PromotionStatus.Active, ct);

    public async Task<int?> GetEmployeeIdByUsernameAsync(string? username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        var employee = await db.Employees
            .Include(e => e.Account)
            .FirstOrDefaultAsync(e => e.Account.Username == username, ct);
        return employee?.EmployeeID;
    }

    public async Task<Order> CreateOrderAsync(
        Customer? selectedCustomer,
        int? employeeId,
        IReadOnlyCollection<PosOrderLine> lines,
        int? promotionId,
        decimal totalAmount,
        decimal discountAmount,
        decimal finalAmount,
        string paymentMethod,
        CancellationToken ct = default)
    {
        var customer = selectedCustomer
            ?? await db.Customers.FirstOrDefaultAsync(c => c.AccountID == null && c.Phone == null, ct);

        if (customer == null)
        {
            customer = new Customer
            {
                LastName = "Khách",
                FirstName = "vãng lai",
                Address = "Mua trực tiếp tại tiệm bánh",
                AccountID = null
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync(ct);
        }

        var productIds = lines.Select(line => line.ProductID).ToArray();
        var stockById = await db.Products
            .Where(p => productIds.Contains(p.ProductID))
            .ToDictionaryAsync(p => p.ProductID, ct);

        foreach (var line in lines)
        {
            if (!stockById.TryGetValue(line.ProductID, out var product)
                || product.Quantity < line.Quantity)
            {
                throw new InvalidOperationException(
                    $"Tồn kho món #{line.ProductID} vừa thay đổi. Vui lòng kiểm tra lại giỏ hàng.");
            }
        }

        var order = new Order
        {
            CustomerID = customer.CustomerID,
            EmployeeID = employeeId,
            PromotionID = promotionId,
            OrderDate = DateTime.Now,
            TotalAmount = totalAmount,
            DiscountAmount = discountAmount,
            FinalAmount = finalAmount,
            PaymentMethod = paymentMethod,
            PaymentStatus = PaymentStatus.Paid,
            Status = OrderStatus.Completed,
            ReceiverName = selectedCustomer?.FullName ?? "Khách vãng lai",
            ReceiverPhone = selectedCustomer?.Phone,
            ShippingAddress = "Nhận tại quầy BakerySystem",
            Note = selectedCustomer != null
                ? $"Bán cho thành viên: {selectedCustomer.FullName}"
                : "Khách vãng lai mua lẻ tại quầy"
        };

        foreach (var line in lines)
        {
            order.OrderDetails.Add(new OrderDetail
            {
                ProductID = line.ProductID,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                DiscountAmount = 0,
                SubTotal = line.SubTotal
            });
            stockById[line.ProductID].Quantity -= line.Quantity;
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        return order;
    }
}
