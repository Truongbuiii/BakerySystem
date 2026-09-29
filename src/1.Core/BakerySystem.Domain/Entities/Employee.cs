namespace BakerySystem.Domain.Entities;

public class Employee
{
    public int EmployeeID { get; set; }
    public int AccountID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }

    // Navigation Properties
    public virtual Account Account { get; set; } = null!;
    public virtual ICollection<ImportReceipt> ImportReceipts { get; set; } = new List<ImportReceipt>();
    public virtual ICollection<Order> HandledOrders { get; set; } = new List<Order>();
}
