namespace BakerySystem.Domain.Entities;

public class ImportReceiptDetail
{
    public int ReceiptID { get; set; }
    public int ProductID { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }

    // Navigation Properties
    public virtual ImportReceipt ImportReceipt { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
