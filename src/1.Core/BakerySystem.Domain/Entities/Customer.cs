using System.ComponentModel.DataAnnotations.Schema;

namespace BakerySystem.Domain.Entities;

public class Customer
{
    public int CustomerID { get; set; }
    public int? AccountID { get; set; }
    public string LastName { get; set; } = string.Empty;   // Họ và tên đệm (VD: Nguyễn Hoàng)
    public string FirstName { get; set; } = string.Empty;  // Tên (VD: Anh)
    public string? Phone { get; set; }
    public string? Address { get; set; }

    // Tiện ích hiển thị họ tên đầy đủ & hỗ trợ tương thích ngược (Không map vào DB)
    [NotMapped]
    public string FullName
    {
        get => string.Join(" ", new[] { LastName, FirstName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                LastName = string.Empty;
                FirstName = string.Empty;
            }
            else
            {
                var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1)
                {
                    FirstName = parts[0];
                    LastName = string.Empty;
                }
                else
                {
                    FirstName = parts[^1];
                    LastName = string.Join(" ", parts[..^1]);
                }
            }
        }
    }

    // Navigation Properties
    public virtual Account? Account { get; set; }
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
