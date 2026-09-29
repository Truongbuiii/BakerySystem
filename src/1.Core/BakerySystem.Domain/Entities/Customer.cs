namespace BakerySystem.Domain.Entities;

public class Customer
{
    public int CustomerID { get; set; }
    public int? AccountID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }

    // Navigation Properties
    public virtual Account? Account { get; set; }
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
