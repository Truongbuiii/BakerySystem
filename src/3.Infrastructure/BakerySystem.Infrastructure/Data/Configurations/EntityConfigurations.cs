using BakerySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BakerySystem.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Account");
        builder.HasKey(a => a.AccountID);
        builder.Property(a => a.Username).HasMaxLength(50).IsRequired().IsUnicode(false);
        builder.HasIndex(a => a.Username).IsUnique();
        builder.Property(a => a.Password).HasMaxLength(255).IsRequired().IsUnicode(false);
        builder.Property(a => a.Role).HasMaxLength(20).IsRequired().IsUnicode(false);
        builder.Property(a => a.Status).HasMaxLength(20).IsRequired().IsUnicode(false).HasDefaultValue("Active");
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customer");
        builder.HasKey(c => c.CustomerID);
        builder.Property(c => c.FullName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(20).IsUnicode(false);
        builder.Property(c => c.Address).HasMaxLength(255);

        builder.HasOne(c => c.Account)
               .WithOne(a => a.Customer)
               .HasForeignKey<Customer>(c => c.AccountID)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.AccountID).IsUnique().HasFilter("[AccountID] IS NOT NULL");
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employee");
        builder.HasKey(e => e.EmployeeID);
        builder.Property(e => e.FullName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(20).IsUnicode(false);

        builder.HasOne(e => e.Account)
               .WithOne(a => a.Employee)
               .HasForeignKey<Employee>(e => e.AccountID)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.AccountID).IsUnique();
    }
}

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Supplier");
        builder.HasKey(s => s.SupplierID);
        builder.Property(s => s.SupplierName).HasMaxLength(150).IsRequired();
        builder.Property(s => s.Phone).HasMaxLength(20).IsUnicode(false);
        builder.Property(s => s.Address).HasMaxLength(255);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Category");
        builder.HasKey(c => c.CategoryID);
        builder.Property(c => c.CategoryName).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => c.CategoryName).IsUnique();
        builder.Property(c => c.IsActive).HasDefaultValue(true);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Product");
        builder.HasKey(p => p.ProductID);
        builder.Property(p => p.ProductName).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.Quantity).HasDefaultValue(0);
        builder.Property(p => p.ImageURL).HasMaxLength(500).IsUnicode(false);
        builder.Property(p => p.IsActive).HasDefaultValue(true);

        builder.HasOne(p => p.Category)
               .WithMany(c => c.Products)
               .HasForeignKey(p => p.CategoryID)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("Promotion");
        builder.HasKey(p => p.PromotionID);
        builder.Property(p => p.PromotionCode).HasMaxLength(50).IsRequired().IsUnicode(false);
        builder.HasIndex(p => p.PromotionCode).IsUnique();
        builder.Property(p => p.PromotionName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.StartDate).HasColumnType("date").IsRequired();
        builder.Property(p => p.EndDate).HasColumnType("date").IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Status).HasMaxLength(20).IsRequired().IsUnicode(false).HasDefaultValue("Active");
    }
}

public class PromotionInvoiceConfiguration : IEntityTypeConfiguration<PromotionInvoice>
{
    public void Configure(EntityTypeBuilder<PromotionInvoice> builder)
    {
        builder.ToTable("PromotionInvoice");
        builder.HasKey(pi => pi.PromotionID);
        builder.Property(pi => pi.PromotionID).ValueGeneratedNever();
        builder.Property(pi => pi.MinOrderAmount).HasPrecision(18, 2).HasDefaultValue(0);
        builder.Property(pi => pi.DiscountPercent).HasPrecision(5, 2).IsRequired();
        builder.Property(pi => pi.MaxDiscountAmount).HasPrecision(18, 2);

        builder.HasOne(pi => pi.Promotion)
               .WithOne(p => p.PromotionInvoice)
               .HasForeignKey<PromotionInvoice>(pi => pi.PromotionID)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PromotionProductConfiguration : IEntityTypeConfiguration<PromotionProduct>
{
    public void Configure(EntityTypeBuilder<PromotionProduct> builder)
    {
        builder.ToTable("PromotionProduct");
        builder.HasKey(pp => new { pp.PromotionID, pp.ProductID });
        builder.Property(pp => pp.DiscountPercent).HasPrecision(5, 2).IsRequired();
        builder.Property(pp => pp.MaxDiscountAmount).HasPrecision(18, 2);

        builder.HasOne(pp => pp.Promotion)
               .WithMany(p => p.PromotionProducts)
               .HasForeignKey(pp => pp.PromotionID)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pp => pp.Product)
               .WithMany(p => p.PromotionProducts)
               .HasForeignKey(pp => pp.ProductID)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ImportReceiptConfiguration : IEntityTypeConfiguration<ImportReceipt>
{
    public void Configure(EntityTypeBuilder<ImportReceipt> builder)
    {
        builder.ToTable("ImportReceipt");
        builder.HasKey(ir => ir.ReceiptID);
        builder.Property(ir => ir.ReceiptDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        builder.Property(ir => ir.TotalAmount).HasPrecision(18, 2).HasDefaultValue(0);
        builder.Property(ir => ir.Note).HasMaxLength(500);

        builder.HasOne(ir => ir.Supplier)
               .WithMany(s => s.ImportReceipts)
               .HasForeignKey(ir => ir.SupplierID)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ir => ir.Employee)
               .WithMany(e => e.ImportReceipts)
               .HasForeignKey(ir => ir.EmployeeID)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ImportReceiptDetailConfiguration : IEntityTypeConfiguration<ImportReceiptDetail>
{
    public void Configure(EntityTypeBuilder<ImportReceiptDetail> builder)
    {
        builder.ToTable("ImportReceiptDetail");
        builder.HasKey(ird => new { ird.ReceiptID, ird.ProductID });
        builder.Property(ird => ird.Quantity).IsRequired();
        builder.Property(ird => ird.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(ird => ird.SubTotal).HasPrecision(18, 2).IsRequired();

        builder.HasOne(ird => ird.ImportReceipt)
               .WithMany(ir => ir.ImportReceiptDetails)
               .HasForeignKey(ird => ird.ReceiptID)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ird => ird.Product)
               .WithMany(p => p.ImportReceiptDetails)
               .HasForeignKey(ird => ird.ProductID)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.OrderID);
        builder.Property(o => o.OrderDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        builder.Property(o => o.TotalAmount).HasPrecision(18, 2).HasDefaultValue(0);
        builder.Property(o => o.DiscountAmount).HasPrecision(18, 2).HasDefaultValue(0);
        builder.Property(o => o.FinalAmount).HasPrecision(18, 2).HasDefaultValue(0);

        builder.Property(o => o.ReceiverName).HasMaxLength(100);
        builder.Property(o => o.ReceiverPhone).HasMaxLength(20).IsUnicode(false);
        builder.Property(o => o.ShippingAddress).HasMaxLength(255);
        builder.Property(o => o.Note).HasMaxLength(500);

        builder.Property(o => o.PaymentMethod).HasMaxLength(30).IsUnicode(false).HasDefaultValue("COD");
        builder.Property(o => o.PaymentStatus).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Unpaid");
        builder.Property(o => o.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Pending");

        builder.HasOne(o => o.Customer)
               .WithMany(c => c.Orders)
               .HasForeignKey(o => o.CustomerID)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Employee)
               .WithMany(e => e.HandledOrders)
               .HasForeignKey(o => o.EmployeeID)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.PromotionInvoice)
               .WithMany(pi => pi.Orders)
               .HasForeignKey(o => o.PromotionID)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

public class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
{
    public void Configure(EntityTypeBuilder<OrderDetail> builder)
    {
        builder.ToTable("OrderDetail");
        builder.HasKey(od => new { od.OrderID, od.ProductID });
        builder.Property(od => od.Quantity).IsRequired();
        builder.Property(od => od.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(od => od.DiscountAmount).HasPrecision(18, 2).HasDefaultValue(0);
        builder.Property(od => od.SubTotal).HasPrecision(18, 2).IsRequired();

        builder.HasOne(od => od.Order)
               .WithMany(o => o.OrderDetails)
               .HasForeignKey(od => od.OrderID)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(od => od.Product)
               .WithMany(p => p.OrderDetails)
               .HasForeignKey(od => od.ProductID)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
