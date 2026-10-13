namespace BakerySystem.Domain.Entities;

public class PromotionOrderTier
{
    public int PromotionID { get; set; }
    public decimal MinOrderAmount { get; set; }      // Mức tiền hóa đơn đạt được để kích hoạt giảm
    public decimal DiscountPercent { get; set; }     // % giảm giá khi đạt mức tiền
    public decimal? DiscountAmount { get; set; }     // Hoặc số tiền giảm cố định (VNĐ)
    public decimal? MaxDiscountAmount { get; set; }  // Mức giảm tối đa để chặn lỗ (VNĐ)

    // Navigation Properties
    public virtual Promotion Promotion { get; set; } = null!;
}

