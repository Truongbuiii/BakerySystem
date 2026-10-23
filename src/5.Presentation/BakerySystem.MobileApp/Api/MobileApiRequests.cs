namespace BakerySystem.MobileApp.Api;

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

public sealed record CreateMobileOrderLine(
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal SubTotal);

public sealed record CancelMobileOrderRequest(int? CustomerId);
