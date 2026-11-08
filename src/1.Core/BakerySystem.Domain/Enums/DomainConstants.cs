namespace BakerySystem.Domain.Enums;

public static class AccountRole
{
    public const string Admin = "Admin";
    public const string Employee = "Employee";
    public const string Shipper = "Shipper";
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
    public const string InProgress = "InProgress";
    public const string Shipping = "Shipping";
    public const string Delivered = "Delivered";
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
    public const string Draft = "Draft";
}

public static class PromotionType
{
    public const string Product = "Product";         // 1. Giảm trực tiếp theo món bánh (PromotionProduct)
    public const string OrderTier = "OrderTier";     // 2. Giảm tự động theo mức tiền hóa đơn đạt được (PromotionOrderTier)
    public const string Invoice = "Invoice";         // 3. Giảm theo mã Voucher hóa đơn (PromotionInvoice)
}

public static class CustomerLoyaltyTier
{
    public const string Diamond = "VIP Kim Cương";
    public const string Gold = "VIP Vàng";
    public const string Silver = "VIP Bạc";
    public const string Standard = "Thành viên Đồng";

    public const decimal DiamondThreshold = 3_000_000m;
    public const decimal GoldThreshold = 1_500_000m;
    public const decimal SilverThreshold = 500_000m;

    public static string GetTierName(decimal totalSpent)
    {
        if (totalSpent >= DiamondThreshold) return Diamond;
        if (totalSpent >= GoldThreshold) return Gold;
        if (totalSpent >= SilverThreshold) return Silver;
        return Standard;
    }

    public static string GetBadgeClass(decimal totalSpent) => totalSpent switch
    {
        >= DiamondThreshold => "bg-purple-100 text-purple-800 dark:bg-purple-950 dark:text-purple-300 border border-purple-300",
        >= GoldThreshold => "bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300 border border-amber-300",
        >= SilverThreshold => "bg-slate-200 text-slate-800 dark:bg-slate-700 dark:text-slate-200 border border-slate-300",
        _ => "bg-orange-50 text-orange-700 dark:bg-orange-950 dark:text-orange-300 border border-orange-200"
    };
}
