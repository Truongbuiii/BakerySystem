namespace BakerySystem.Domain.Entities;

public class PromotionInvoice
{
    public int PromotionID { get; set; }
    public decimal MinOrderAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal? MaxDiscountAmount { get; set; }

    // Navigation Properties
    public virtual Promotion Promotion { get; set; } = null!;
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
