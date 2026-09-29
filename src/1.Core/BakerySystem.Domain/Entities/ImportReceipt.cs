namespace BakerySystem.Domain.Entities;

public class ImportReceipt
{
    public int ReceiptID { get; set; }
    public int SupplierID { get; set; }
    public int EmployeeID { get; set; }
    public DateTime ReceiptDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }

    // Navigation Properties
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual Employee Employee { get; set; } = null!;
    public virtual ICollection<ImportReceiptDetail> ImportReceiptDetails { get; set; } = new List<ImportReceiptDetail>();
}
