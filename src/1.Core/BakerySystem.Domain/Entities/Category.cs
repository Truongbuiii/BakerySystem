namespace BakerySystem.Domain.Entities;

public class Category
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
