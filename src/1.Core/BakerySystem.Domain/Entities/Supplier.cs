using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace BakerySystem.Domain.Entities;

public class Supplier
{
    public int SupplierID { get; set; }
    [NotMapped]
    public string SupplierCode => $"NCC{SupplierID.ToString("D4", CultureInfo.InvariantCulture)}";
    public string SupplierName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual ICollection<ImportReceipt> ImportReceipts { get; set; } = new List<ImportReceipt>();
    public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();
}
