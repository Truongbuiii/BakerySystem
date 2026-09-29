namespace BakerySystem.Domain.Entities;

public class Order
{
    public int OrderID { get; set; }
    public int CustomerID { get; set; }
    public int? EmployeeID { get; set; }
    public int? PromotionID { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }

    // Shipping & Cake custom notes
    public string? ReceiverName { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? ShippingAddress { get; set; }
    public string? Note { get; set; } // Ghi chữ lên bánh, dặn dò thợ làm bánh

    // Payment & Status
    public string PaymentMethod { get; set; } = "COD";
    public string PaymentStatus { get; set; } = "Unpaid";
    public string Status { get; set; } = "Pending";

    // Navigation Properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual Employee? Employee { get; set; }
    public virtual PromotionInvoice? PromotionInvoice { get; set; }
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
