namespace BakerySystem.Domain.Entities;

public class Promotion
{
    public int PromotionID { get; set; }
    public string PromotionCode { get; set; } = string.Empty;
    public string PromotionName { get; set; } = string.Empty;
    public string PromotionType { get; set; } = "Invoice"; // "Product", "OrderTier", "Invoice"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Active";

    // Navigation Properties
    public virtual PromotionInvoice? PromotionInvoice { get; set; }
    public virtual PromotionOrderTier? PromotionOrderTier { get; set; }
    public virtual ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
}
