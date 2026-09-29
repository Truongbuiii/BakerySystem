namespace BakerySystem.Domain.Entities;

public class Account
{
    public int AccountID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer";
    public string Status { get; set; } = "Active";

    // Navigation Properties
    public virtual Customer? Customer { get; set; }
    public virtual Employee? Employee { get; set; }
}
