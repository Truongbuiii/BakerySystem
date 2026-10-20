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
    public Task<List<Promotion>> GetAsync(CancellationToken ct = default) => db.Promotions.AsNoTracking()
        .Include(p => p.PromotionInvoice)
        .Include(p => p.PromotionOrderTier)
        .Include(p => p.PromotionProducts).ThenInclude(x => x.Product)
        .OrderByDescending(p => p.StartDate).ToListAsync(ct);

    public async Task SaveAsync(
        Promotion campaign,
        bool useInvoiceCondition,
        bool useProductCondition,
        decimal invoicePercent,
        decimal minimum,
        decimal? invoiceCap,
        IReadOnlyCollection<int> productIds,
        decimal productPercent,
        decimal? productCap,
        CancellationToken ct = default)
    {
        campaign.PromotionCode = campaign.PromotionCode.Trim().ToUpperInvariant();
        if (campaign.PromotionCode.Length == 0 || campaign.StartDate.Date > campaign.EndDate.Date ||
            (!useInvoiceCondition && !useProductCondition) ||
            (useInvoiceCondition && (invoicePercent <= 0 || invoicePercent > 100 || minimum < 0 || invoiceCap < 0)) ||
            (useProductCondition && (productIds.Count == 0 || productPercent <= 0 || productPercent > 100 || productCap < 0)))
            throw new InvalidOperationException("Vui lòng chọn ít nhất một điều kiện và kiểm tra các giá trị đã nhập.");
        if (await db.Promotions.AnyAsync(p => p.PromotionCode == campaign.PromotionCode, ct)) throw new InvalidOperationException("Mã chương trình đã tồn tại.");
        campaign.PromotionType = (useInvoiceCondition, useProductCondition) switch
        {
            (true, true) => "InvoiceProduct",
            (true, false) => "Invoice",
            _ => "Product"
        };
        campaign.Status = "Active";
        db.Promotions.Add(campaign);
        if (useInvoiceCondition)
            campaign.PromotionInvoice = new PromotionInvoice
            {
                Promotion = campaign,
                VoucherCode = campaign.PromotionCode,
                DiscountPercent = invoicePercent,
                MinOrderAmount = minimum,
                MaxDiscountAmount = invoiceCap
            };
        if (useProductCondition)
            foreach (var productId in productIds.Distinct())
                campaign.PromotionProducts.Add(new PromotionProduct
                {
                    Promotion = campaign,
                    ProductID = productId,
                    DiscountPercent = productPercent,
                    MaxDiscountAmount = productCap
                });
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> HasBeenUsedAsync(int promotionId, CancellationToken ct = default) =>
        db.Orders.AnyAsync(o => o.PromotionID == promotionId, ct);

    public async Task SetStatusAsync(int promotionId, string status, CancellationToken ct = default)
    {
        var promotion = await db.Promotions.FindAsync([promotionId], ct)
            ?? throw new InvalidOperationException("Không tìm thấy chương trình khuyến mãi.");
        if (status is not ("Active" or "Hidden"))
            throw new InvalidOperationException("Trạng thái chương trình không hợp lệ.");
        promotion.Status = status;
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(
        Promotion campaign,
        bool useInvoiceCondition,
        bool useProductCondition,
        decimal invoicePercent,
        decimal minimum,
        decimal? invoiceCap,
        IReadOnlyCollection<int> productIds,
        decimal productPercent,
        decimal? productCap,
        bool hasBeenUsed,
        CancellationToken ct = default)
    {
        var current = await db.Promotions
            .Include(p => p.PromotionInvoice)
            .Include(p => p.PromotionProducts)
            .FirstOrDefaultAsync(p => p.PromotionID == campaign.PromotionID, ct)
            ?? throw new InvalidOperationException("Không tìm thấy chương trình khuyến mãi.");
        var originalCode = current.PromotionCode;

        campaign.PromotionCode = campaign.PromotionCode.Trim().ToUpperInvariant();
        if (campaign.PromotionCode.Length == 0 || campaign.StartDate.Date > campaign.EndDate.Date)
            throw new InvalidOperationException("Vui lòng kiểm tra mã và thời hạn.");
        if (await db.Promotions.AnyAsync(p => p.PromotionID != campaign.PromotionID && p.PromotionCode == campaign.PromotionCode, ct))
            throw new InvalidOperationException("Mã chương trình đã tồn tại.");

        current.PromotionName = string.IsNullOrWhiteSpace(campaign.PromotionName) ? campaign.PromotionCode : campaign.PromotionName.Trim();
        current.Description = campaign.Description?.Trim();
        current.StartDate = campaign.StartDate;
        current.EndDate = campaign.EndDate;

        if (!hasBeenUsed)
        {
            if (!useInvoiceCondition && !useProductCondition ||
                useInvoiceCondition && (invoicePercent <= 0 || invoicePercent > 100 || minimum < 0 || invoiceCap < 0) ||
                useProductCondition && (productIds.Count == 0 || productPercent <= 0 || productPercent > 100 || productCap < 0))
                throw new InvalidOperationException("Vui lòng chọn ít nhất một điều kiện và kiểm tra các giá trị đã nhập.");

            current.PromotionCode = campaign.PromotionCode;
            current.PromotionType = (useInvoiceCondition, useProductCondition) switch
            {
                (true, true) => "InvoiceProduct",
                (true, false) => "Invoice",
                _ => "Product"
            };
            if (current.PromotionInvoice is not null)
                db.PromotionInvoices.Remove(current.PromotionInvoice);
            if (current.PromotionProducts.Count > 0)
                db.PromotionProducts.RemoveRange(current.PromotionProducts);
            if (useInvoiceCondition)
                current.PromotionInvoice = new PromotionInvoice
                {
                    Promotion = current,
                    VoucherCode = campaign.PromotionCode,
                    DiscountPercent = invoicePercent,
                    MinOrderAmount = minimum,
                    MaxDiscountAmount = invoiceCap
                };
            if (useProductCondition)
                foreach (var productId in productIds.Distinct())
                    current.PromotionProducts.Add(new PromotionProduct
                    {
                        Promotion = current,
                        ProductID = productId,
                        DiscountPercent = productPercent,
                        MaxDiscountAmount = productCap
                    });
        }
        else if (originalCode != campaign.PromotionCode)
        {
            throw new InvalidOperationException("Không thể đổi mã hoặc điều kiện của chương trình đã có đơn hàng sử dụng.");
        }

        await db.SaveChangesAsync(ct);
    }
}
