using BakerySystem.Domain.Entities;

namespace BakerySystem.API;

/// <summary>Payload used to create an order from the mobile application.</summary>
public sealed record CreateMobileOrderRequest(
    int? CustomerId,
    string ReceiverName,
    string Phone,
    string Address,
    string Note,
    string PaymentMethod,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal FinalAmount,
    int? PromotionId,
    IReadOnlyList<CreateMobileOrderLine> Lines);

/// <summary>One product line in a mobile order.</summary>
public sealed record CreateMobileOrderLine(
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal SubTotal);

/// <summary>Payload used to cancel a pending mobile order.</summary>
public sealed record CancelMobileOrderRequest(int? CustomerId);

/// <summary>Payload used to update a mobile customer profile.</summary>
public sealed record MobileProfileRequest(
    int CustomerId,
    string FullName,
    string? Phone,
    string? Address);
