namespace BakerySystem.Domain.Entities;

public class Supplier
{
    public int SupplierID { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }

    // Navigation Properties
    public virtual ICollection<ImportReceipt> ImportReceipts { get; set; } = new List<ImportReceipt>();
}
