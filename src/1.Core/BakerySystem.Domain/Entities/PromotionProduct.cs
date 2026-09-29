namespace BakerySystem.Domain.Entities;

public class PromotionProduct
{
    public int PromotionID { get; set; }
    public int ProductID { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal? MaxDiscountAmount { get; set; }

    // Navigation Properties
    public virtual Promotion Promotion { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
