namespace BakerySystem.Domain.Entities;

public class PromotionInvoice
{
    public int PromotionID { get; set; }
    public string? VoucherCode { get; set; }          // Mã voucher nhập tay (VD: BAKERY2026)
    public decimal MinOrderAmount { get; set; }      // Mức đơn tối thiểu để áp dụng mã
    public decimal DiscountPercent { get; set; }     // % giảm giá
    public decimal? DiscountAmount { get; set; }     // Hoặc số tiền giảm cố định (VNĐ)
    public decimal? MaxDiscountAmount { get; set; }  // Mức giảm tối đa (VNĐ)
    public int? UsageLimit { get; set; }             // Giới hạn lượt sử dụng mã
    public int UsedCount { get; set; } = 0;          // Số lượt đã áp dụng mã

    // Navigation Properties
    public virtual Promotion Promotion { get; set; } = null!;
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
