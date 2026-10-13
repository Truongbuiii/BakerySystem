namespace BakerySystem.Domain.Enums;

public static class AccountRole
{
    public const string Admin = "Admin";
    public const string Employee = "Employee";
    public const string Customer = "Customer";
}

public static class AccountStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
}

public static class OrderStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Shipping = "Shipping";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}

public static class PaymentMethod
{
    public const string COD = "COD";
    public const string BankTransfer = "BankTransfer";
    public const string VNPAY = "VNPAY";
    public const string MoMo = "MoMo";
}

public static class PaymentStatus
{
    public const string Unpaid = "Unpaid";
    public const string Paid = "Paid";
    public const string Refunded = "Refunded";
}

public static class PromotionStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
}

public static class PromotionType
{
    public const string Product = "Product";         // 1. Giảm trực tiếp theo món bánh (PromotionProduct)
    public const string OrderTier = "OrderTier";     // 2. Giảm tự động theo mức tiền hóa đơn đạt được (PromotionOrderTier)
    public const string Invoice = "Invoice";         // 3. Giảm theo mã Voucher hóa đơn (PromotionInvoice)
}

