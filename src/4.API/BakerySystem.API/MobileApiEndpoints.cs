using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Application.Features.Auth;
using BakerySystem.Domain.Entities;
using BakerySystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.API;

public static class MobileApiEndpoints
{
    public static IEndpointRouteBuilder MapMobileApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/mobile")
            .WithTags("Mobile");

        group.MapPost("/auth/login", async (
            LoginRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var result = await authService.CustomerLoginAsync(request);
            return Results.Ok(result);
        })
        .WithSummary("Authenticate a mobile customer");

        group.MapPost("/auth/register", async (
            CustomerRegisterRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var result = await authService.RegisterCustomerAsync(request);
            return Results.Ok(result);
        })
        .WithSummary("Register a mobile customer");

        group.MapGet("/customers/{customerId:int}", async (
            int customerId,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var customer = await authService.GetCustomerProfileAsync(customerId);
            return customer is null ? Results.NotFound() : Results.Ok(customer);
        })
        .WithSummary("Get a customer profile");

        group.MapPut("/customers/profile", async (
            MobileProfileRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var updated = await authService.UpdateCustomerProfileAsync(
                new UpdateCustomerProfileRequest
                {
                    CustomerId = request.CustomerId,
                    FullName = request.FullName,
                    Phone = request.Phone,
                    Address = request.Address
                });
            return updated ? Results.Ok() : Results.NotFound();
        })
        .WithSummary("Update a customer profile");

        group.MapPost("/customers/change-password", async (
            ChangePasswordRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var changed = await authService.ChangePasswordAsync(request);
            return changed ? Results.Ok() : Results.BadRequest();
        })
        .WithSummary("Change a customer password");

        group.MapGet("/categories", async (
            IBakeryDbContext db,
            CancellationToken ct) =>
            Results.Ok(await db.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync(ct)))
        .WithSummary("Get product categories");

        group.MapGet("/products", async (
            IBakeryDbContext db,
            CancellationToken ct) =>
            Results.Ok(await db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProductName)
                .ToListAsync(ct)))
        .WithSummary("Get active products");

        group.MapGet("/promotions", async (
            IBakeryDbContext db,
            CancellationToken ct) =>
        {
            var today = DateTime.Today;
            var promotions = await db.Promotions
                .AsNoTracking()
                .Include(p => p.PromotionInvoice)
                .Include(p => p.PromotionProducts)
                    .ThenInclude(pp => pp.Product)
                .Where(p => p.Status == PromotionStatus.Active
                    && p.StartDate <= today
                    && p.EndDate >= today
                    && (p.PromotionInvoice == null
                        || p.PromotionInvoice.UsageLimit == null
                        || p.PromotionInvoice.UsedCount < p.PromotionInvoice.UsageLimit))
                .OrderByDescending(p => p.PromotionInvoice != null
                    ? p.PromotionInvoice.DiscountPercent
                    : 0)
                .ToListAsync(ct);
            return Results.Ok(promotions);
        })
        .WithSummary("Get active promotions");

        group.MapGet("/promotions/{code}", async (
            string code,
            IBakeryDbContext db,
            CancellationToken ct) =>
        {
            var promotion = await db.Promotions
                .AsNoTracking()
                .Include(p => p.PromotionInvoice)
                .Include(p => p.PromotionProducts)
                    .ThenInclude(pp => pp.Product)
                .FirstOrDefaultAsync(p => p.PromotionCode.ToUpper() == code.Trim().ToUpper()
                    && p.Status == PromotionStatus.Active
                    && p.StartDate <= DateTime.Today
                    && p.EndDate >= DateTime.Today
                    && (p.PromotionInvoice == null
                        || p.PromotionInvoice.UsageLimit == null
                        || p.PromotionInvoice.UsedCount < p.PromotionInvoice.UsageLimit), ct);
            return promotion is null ? Results.NotFound() : Results.Ok(promotion);
        })
        .WithSummary("Find a promotion by code");

        group.MapGet("/orders", async (
            int? customerId,
            string? phone,
            IBakeryDbContext db,
            CancellationToken ct) =>
        {
            var query = db.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .AsQueryable();

            if (customerId.HasValue)
                query = query.Where(o => o.CustomerID == customerId.Value);
            else if (!string.IsNullOrWhiteSpace(phone))
                query = query.Where(o => o.ReceiverPhone != null
                    && o.ReceiverPhone.Contains(phone.Trim()));

            return Results.Ok(await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(ct));
        })
        .WithSummary("Get mobile customer orders");

        group.MapPost("/orders", async (
            CreateMobileOrderRequest request,
            IBakeryDbContext db,
            CancellationToken ct) =>
        {
            if (request.Lines.Count == 0)
                return Results.BadRequest("Order must contain at least one line.");

            var customer = request.CustomerId is > 0
                ? await db.Customers.FirstOrDefaultAsync(
                    c => c.CustomerID == request.CustomerId.Value, ct)
                : await db.Customers.FirstOrDefaultAsync(
                    c => c.Phone == request.Phone, ct);

            if (customer is null)
                return Results.BadRequest("Customer was not found. Please sign in or register before ordering.");

            var submittedLines = request.Lines
                .GroupBy(line => line.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    Quantity = group.Sum(line => line.Quantity)
                })
                .ToList();
            if (submittedLines.Any(line => line.Quantity <= 0))
                return Results.BadRequest("Product quantities must be greater than zero.");

            var productIds = submittedLines.Select(line => line.ProductId).ToArray();
            var products = await db.Products
                .Where(p => productIds.Contains(p.ProductID) && p.IsActive)
                .ToDictionaryAsync(p => p.ProductID, ct);

            decimal totalAmount = 0;
            var orderLines = new List<OrderDetail>();
            foreach (var line in submittedLines)
            {
                if (!products.TryGetValue(line.ProductId, out var product)
                    || line.Quantity <= 0
                    || product.Quantity < line.Quantity)
                    return Results.Conflict($"Insufficient stock for product {line.ProductId}.");

                var subTotal = product.Price * line.Quantity;
                totalAmount += subTotal;
                orderLines.Add(new OrderDetail
                {
                    ProductID = product.ProductID,
                    Quantity = line.Quantity,
                    UnitPrice = product.Price,
                    SubTotal = subTotal
                });
            }

            decimal discountAmount = 0;
            Promotion? promotion = null;
            if (request.PromotionId.HasValue)
            {
                var today = DateTime.Today;
                promotion = await db.Promotions
                    .Include(p => p.PromotionInvoice)
                    .Include(p => p.PromotionProducts)
                    .FirstOrDefaultAsync(p => p.PromotionID == request.PromotionId.Value
                        && p.Status == PromotionStatus.Active
                        && p.StartDate <= today
                        && p.EndDate >= today, ct);

                if (promotion is null
                    || promotion.PromotionInvoice is { UsageLimit: not null } invoice
                        && invoice.UsedCount >= invoice.UsageLimit)
                    return Results.BadRequest("Promotion is inactive, expired, or no longer available.");

                if (promotion.PromotionInvoice is { } invoiceCondition
                    && totalAmount >= invoiceCondition.MinOrderAmount)
                {
                    var invoiceDiscount = totalAmount * invoiceCondition.DiscountPercent / 100m;
                    if (invoiceCondition.MaxDiscountAmount is > 0)
                        invoiceDiscount = Math.Min(invoiceDiscount, invoiceCondition.MaxDiscountAmount.Value);
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
                    discountAmount += Math.Min(line.SubTotal, productDiscount);
                }

                if (discountAmount <= 0)
                    return Results.BadRequest("Promotion conditions are not met for the products in this order.");
            }
            discountAmount = Math.Min(totalAmount, discountAmount);

            var order = new Order
            {
                CustomerID = customer.CustomerID,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                DiscountAmount = discountAmount,
                FinalAmount = totalAmount - discountAmount,
                ReceiverName = request.ReceiverName,
                ReceiverPhone = request.Phone,
                ShippingAddress = request.Address,
                Note = request.Note,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = request.PaymentMethod == PaymentMethod.COD
                    ? PaymentStatus.Unpaid
                    : PaymentStatus.Paid,
                Status = OrderStatus.Pending,
                PromotionID = promotion?.PromotionID
            };

            foreach (var orderLine in orderLines)
                order.OrderDetails.Add(orderLine);

            if (promotion?.PromotionInvoice is { } usedInvoice)
                usedInvoice.UsedCount++;

            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
            return Results.Ok(order);
        })
        .WithSummary("Create a mobile order");

        group.MapPost("/orders/{orderId:int}/cancel", async (
            int orderId,
            CancelMobileOrderRequest request,
            IBakeryDbContext db,
            CancellationToken ct) =>
        {
            var order = await db.Orders.FirstOrDefaultAsync(
                o => o.OrderID == orderId, ct);
            if (order is null
                || request.CustomerId is > 0 && order.CustomerID != request.CustomerId
                || order.Status != OrderStatus.Pending)
                return Results.BadRequest();

            order.Status = OrderStatus.Cancelled;
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        })
        .WithSummary("Cancel a pending mobile order");

        return endpoints;
    }
}
