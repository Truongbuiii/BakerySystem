using BakerySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.Application.Common.Interfaces;

public interface IBakeryDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Employee> Employees { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<Promotion> Promotions { get; }
    DbSet<PromotionInvoice> PromotionInvoices { get; }
    DbSet<PromotionProduct> PromotionProducts { get; }
    DbSet<ImportReceipt> ImportReceipts { get; }
    DbSet<ImportReceiptDetail> ImportReceiptDetails { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderDetail> OrderDetails { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
