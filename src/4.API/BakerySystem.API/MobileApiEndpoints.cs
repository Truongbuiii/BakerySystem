using System.Globalization;
using System.Security.Claims;
using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Application.Features.Auth;
using BakerySystem.Domain.Entities;
using BakerySystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.API;

public static class MobileApiEndpoints
{
    private static bool TryGetAuthenticatedCustomer(ClaimsPrincipal user, out int customerId, out int accountId)
    {
        customerId = 0;
        accountId = 0;

        var customerClaim = user.FindFirst(JwtTokenService.CustomerIdClaim)?.Value;
        var accountClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(customerClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out customerId)
            && customerId > 0
            && int.TryParse(accountClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out accountId)
            && accountId > 0;
    }

    public static IEndpointRouteBuilder MapMobileApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/mobile")
            .WithTags("Mobile");

        group.MapPost("/auth/login", async (
            LoginRequest request,
            IAuthService authService,
            JwtTokenService tokenService,
            CancellationToken ct) =>
        {
            var result = await authService.CustomerLoginAsync(request);
            if (result.Success && result.Customer?.AccountId is int accountId)
            {
                var sessionStamp = await authService.GetSessionStampAsync(accountId, ct);
                result.AccessToken = tokenService.CreateCustomerToken(result.Customer, sessionStamp);
            }
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithSummary("Authenticate a mobile customer");

        group.MapPost("/auth/register", async (
            CustomerRegisterRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var result = await authService.RegisterCustomerAsync(request);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithSummary("Register a mobile customer");

        group.MapGet("/customers/{customerId:int}", async (
            int customerId,
            ClaimsPrincipal user,
            IAuthService authService,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedCustomer(user, out var authCustomerId, out _))
                return Results.Unauthorized();

            if (customerId != authCustomerId)
                return Results.Forbid();

            var customer = await authService.GetCustomerProfileAsync(authCustomerId);
            return customer is null ? Results.NotFound() : Results.Ok(customer);
        })
        .RequireAuthorization("MobileCustomer")
        .WithSummary("Get a customer profile");

        group.MapPut("/customers/profile", async (
            MobileProfileRequest request,
            ClaimsPrincipal user,
            IAuthService authService,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedCustomer(user, out var authCustomerId, out _))
                return Results.Unauthorized();

            if (request.CustomerId > 0 && request.CustomerId != authCustomerId)
                return Results.Forbid();

            try
            {
                var updated = await authService.UpdateCustomerProfileAsync(
                    new UpdateCustomerProfileRequest
                    {
                        CustomerId = authCustomerId,
                        FullName = request.FullName,
                        Phone = request.Phone,
                        Address = request.Address
                    });
                return updated ? Results.Ok() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .RequireAuthorization("MobileCustomer")
        .WithSummary("Update a customer profile");

        group.MapPost("/customers/change-password", async (
            ChangePasswordRequest request,
            ClaimsPrincipal user,
            IAuthService authService,
            JwtTokenService tokenService,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedCustomer(user, out var authCustomerId, out var authAccountId))
                return Results.Unauthorized();

            if (request.AccountId > 0 && request.AccountId != authAccountId)
                return Results.Forbid();

            var changed = await authService.ChangePasswordAsync(new ChangePasswordRequest
            {
                AccountId = authAccountId,
                OldPassword = request.OldPassword,
                NewPassword = request.NewPassword,
                ConfirmPassword = request.ConfirmPassword
            });
            if (!changed)
                return Results.BadRequest();

            var customer = await authService.GetCustomerProfileAsync(authCustomerId);
            if (customer is null || customer.AccountId is null)
                return Results.Ok();

            var newStamp = await authService.GetSessionStampAsync(customer.AccountId.Value, ct);
            var newToken = tokenService.CreateCustomerToken(customer, newStamp);
            return Results.Ok(new { accessToken = newToken });
        })
        .RequireAuthorization("MobileCustomer")
        .WithSummary("Change a customer password");

        group.MapGet("/categories", async (
            IBakeryDbContext db,
            CancellationToken ct) =>
            Results.Ok(await db.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync(ct)))
        .AllowAnonymous()
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
        .AllowAnonymous()
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
                    ? (p.PromotionInvoice.DiscountAmount ?? p.PromotionInvoice.DiscountPercent)
                    : 0)
                .ToListAsync(ct);
            return Results.Ok(promotions);
        })
        .AllowAnonymous()
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
        .AllowAnonymous()
        .WithSummary("Find a promotion by code");

        group.MapGet("/orders", async (
            int? customerId,
            ClaimsPrincipal user,
            IBakeryDbContext db,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedCustomer(user, out var authCustomerId, out _))
                return Results.Unauthorized();

            if (customerId is > 0 && customerId.Value != authCustomerId)
                return Results.Forbid();

            var query = db.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Where(o => o.CustomerID == authCustomerId);

            return Results.Ok(await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(ct));
        })
        .RequireAuthorization("MobileCustomer")
        .WithSummary("Get mobile customer orders");

        group.MapPost("/orders", async (
            CreateMobileOrderRequest request,
            ClaimsPrincipal user,
            IBakeryDbContext db,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedCustomer(user, out var authCustomerId, out _))
                return Results.Unauthorized();

            if (request.CustomerId is > 0 && request.CustomerId.Value != authCustomerId)
                return Results.Forbid();

            if (request.Lines.Count == 0)
                return Results.BadRequest("Order must contain at least one line.");

            var customer = await db.Customers.FirstOrDefaultAsync(
                c => c.CustomerID == authCustomerId, ct);

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

                var linkedPromotionOrderCount = promotion is null
                    ? 0
                    : await db.Orders.CountAsync(
                        order => order.PromotionID == promotion.PromotionID
                            && order.Status != OrderStatus.Cancelled, ct);

                if (promotion is null
                    || promotion.PromotionInvoice is { UsageLimit: not null } invoice
                        && linkedPromotionOrderCount >= invoice.UsageLimit)
                    return Results.BadRequest("Promotion is inactive, expired, or no longer available.");

                if (promotion.PromotionInvoice is { } invoiceCondition
                    && totalAmount >= invoiceCondition.MinOrderAmount)
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
            {
                var linkedPromotionOrderCount = await db.Orders.CountAsync(
                    order => order.PromotionID == promotion.PromotionID
                        && order.Status != OrderStatus.Cancelled, ct);
                usedInvoice.UsedCount = linkedPromotionOrderCount + 1;
            }

            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
            return Results.Ok(order);
        })
        .RequireAuthorization("MobileCustomer")
        .WithSummary("Create a mobile order");

        group.MapPost("/orders/{orderId:int}/cancel", async (
            int orderId,
            CancelMobileOrderRequest? request,
            ClaimsPrincipal user,
            IBakeryDbContext db,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedCustomer(user, out var authCustomerId, out _))
                return Results.Unauthorized();

            if (request?.CustomerId is > 0 && request.CustomerId.Value != authCustomerId)
                return Results.Forbid();

            var order = await db.Orders.FirstOrDefaultAsync(
                o => o.OrderID == orderId, ct);
            if (order is null)
                return Results.NotFound();

            if (order.CustomerID != authCustomerId)
                return Results.Forbid();

            if (order.Status != OrderStatus.Pending)
                return Results.BadRequest("Only pending orders can be cancelled.");

            order.Status = OrderStatus.Cancelled;

            if (order.PromotionID.HasValue)
            {
                var promoInvoice = await db.PromotionInvoices.FirstOrDefaultAsync(
                    pi => pi.PromotionID == order.PromotionID.Value, ct);
                if (promoInvoice is not null)
                {
                    promoInvoice.UsedCount = await db.Orders.CountAsync(
                        o => o.PromotionID == order.PromotionID.Value
                            && o.OrderID != order.OrderID
                            && o.Status != OrderStatus.Cancelled, ct);
                }
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok();
        })
        .RequireAuthorization("MobileCustomer")
        .WithSummary("Cancel a pending mobile order");

        group.MapPost("/orders/{orderId:int}/confirm-received", async (
            int orderId,
            ConfirmReceivedMobileOrderRequest? request,
            ClaimsPrincipal user,
            IBakeryDbContext db,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedCustomer(user, out var authCustomerId, out _))
                return Results.Unauthorized();

            if (request?.CustomerId is > 0 && request.CustomerId.Value != authCustomerId)
                return Results.Forbid();

            var order = await db.Orders.FirstOrDefaultAsync(
                o => o.OrderID == orderId, ct);
            if (order is null)
                return Results.NotFound();

            if (order.CustomerID != authCustomerId)
                return Results.Forbid();

            if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Shipping)
                return Results.BadRequest("Order cannot be confirmed at current status.");

            order.Status = OrderStatus.Completed;
            order.PaymentStatus = PaymentStatus.Paid;
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        })
        .RequireAuthorization("MobileCustomer")
        .WithSummary("Confirm that customer received the order");

        return endpoints;
    }
}
