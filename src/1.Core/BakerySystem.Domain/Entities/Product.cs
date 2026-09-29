namespace BakerySystem.Domain.Entities;

public class Product
{
    public int ProductID { get; set; }
    public int CategoryID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? ImageURL { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
    public virtual ICollection<ImportReceiptDetail> ImportReceiptDetails { get; set; } = new List<ImportReceiptDetail>();
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
