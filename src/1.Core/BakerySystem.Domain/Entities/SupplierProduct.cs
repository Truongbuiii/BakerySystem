namespace BakerySystem.Domain.Entities;

public class SupplierProduct
{
    public int SupplierID { get; set; }
    public int ProductID { get; set; }
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
