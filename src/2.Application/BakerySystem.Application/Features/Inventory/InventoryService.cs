using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.Application.Features.Inventory;

public sealed class ImportLineInput { public int ProductID { get; set; } public int Quantity { get; set; } public decimal UnitPrice { get; set; } }
public sealed class ImportReceiptInput { public int SupplierID { get; set; } public int EmployeeID { get; set; } public string? Note { get; set; } public List<ImportLineInput> Lines { get; set; } = []; }
public sealed class InventoryService(IBakeryDbContext db)
{
    public Task<List<Supplier>> GetSuppliersAsync(CancellationToken ct = default) => db.Suppliers.AsNoTracking().OrderBy(x => x.SupplierName).ToListAsync(ct);
    public async Task SaveSupplierAsync(Supplier item, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(item.SupplierName)) throw new InvalidOperationException("Vui lòng nhập tên nhà cung cấp.");
        if (item.SupplierID == 0) db.Suppliers.Add(item);
        else { var current = await db.Suppliers.FindAsync([item.SupplierID], ct) ?? throw new InvalidOperationException("Không tìm thấy nhà cung cấp."); current.SupplierName = item.SupplierName.Trim(); current.Phone = item.Phone?.Trim(); current.Address = item.Address?.Trim(); }
        await db.SaveChangesAsync(ct);
    }
    public async Task SaveReceiptAsync(ImportReceiptInput input, CancellationToken ct = default)
    {
        var lines = input.Lines.Where(x => x.ProductID > 0 && x.Quantity > 0).GroupBy(x => x.ProductID).Select(g => new ImportLineInput { ProductID = g.Key, Quantity = g.Sum(x => x.Quantity), UnitPrice = g.Last().UnitPrice }).ToList();
        if (input.SupplierID <= 0 || lines.Count == 0 || lines.Any(x => x.UnitPrice < 0)) throw new InvalidOperationException("Chọn nhà cung cấp và ít nhất một sản phẩm có số lượng hợp lệ.");
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.EmployeeID == input.EmployeeID, ct) ?? await db.Employees.OrderBy(e => e.EmployeeID).FirstOrDefaultAsync(ct);
        if (employee is null) throw new InvalidOperationException("Không tìm thấy nhân viên lập phiếu.");
        var products = await db.Products.Where(p => lines.Select(x => x.ProductID).Contains(p.ProductID)).ToDictionaryAsync(p => p.ProductID, ct);
        if (products.Count != lines.Count) throw new InvalidOperationException("Một số sản phẩm không còn tồn tại.");
        var receipt = new ImportReceipt { SupplierID = input.SupplierID, EmployeeID = employee.EmployeeID, ReceiptDate = DateTime.Now, Note = input.Note?.Trim(), TotalAmount = lines.Sum(x => x.Quantity * x.UnitPrice) };
        foreach (var line in lines) { products[line.ProductID].Quantity = checked(products[line.ProductID].Quantity + line.Quantity); receipt.ImportReceiptDetails.Add(new ImportReceiptDetail { ProductID = line.ProductID, Quantity = line.Quantity, UnitPrice = line.UnitPrice, SubTotal = line.Quantity * line.UnitPrice }); }
        db.ImportReceipts.Add(receipt);
        await db.SaveChangesAsync(ct);
    }
    public Task<List<ImportReceipt>> GetReceiptsAsync(CancellationToken ct = default) => db.ImportReceipts.AsNoTracking().Include(x => x.Supplier).Include(x => x.Employee).Include(x => x.ImportReceiptDetails).ThenInclude(x => x.Product).OrderByDescending(x => x.ReceiptDate).ToListAsync(ct);
    public Task<List<Product>> GetProductsAsync(CancellationToken ct = default) => db.Products.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.ProductName).ToListAsync(ct);
}

public sealed class PromotionService(IBakeryDbContext db)
{
    public Task<List<Promotion>> GetAsync(CancellationToken ct = default) => db.Promotions.AsNoTracking().Include(p => p.PromotionInvoice).OrderByDescending(p => p.StartDate).ToListAsync(ct);
    public async Task SaveAsync(Promotion campaign, decimal percent, decimal minimum, decimal? cap, CancellationToken ct = default)
    {
        campaign.PromotionCode = campaign.PromotionCode.Trim().ToUpperInvariant();
        if (campaign.PromotionCode.Length == 0 || campaign.StartDate.Date > campaign.EndDate.Date || percent <= 0 || percent > 100 || minimum < 0 || cap < 0) throw new InvalidOperationException("Vui lòng kiểm tra mã, thời hạn và điều kiện giảm giá.");
        if (await db.Promotions.AnyAsync(p => p.PromotionCode == campaign.PromotionCode, ct)) throw new InvalidOperationException("Mã chương trình đã tồn tại.");
        campaign.PromotionType = "Invoice";
        campaign.Status = "Active";
        db.Promotions.Add(campaign);
        campaign.PromotionInvoice = new PromotionInvoice { Promotion = campaign, VoucherCode = campaign.PromotionCode, DiscountPercent = percent, MinOrderAmount = minimum, MaxDiscountAmount = cap };
        await db.SaveChangesAsync(ct);
    }
}
