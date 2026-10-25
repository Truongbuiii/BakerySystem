using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Domain.Entities;
using BakerySystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.Application.Features.Inventory;

public sealed class ImportLineInput { public int ProductID { get; set; } public int Quantity { get; set; } public decimal UnitPrice { get; set; } }
public sealed class ImportReceiptInput { public int SupplierID { get; set; } public int EmployeeID { get; set; } public string? Note { get; set; } public List<ImportLineInput> Lines { get; set; } = []; }
public sealed class InventoryService(IBakeryDbContext db)
{
    public Task<List<Supplier>> GetSuppliersAsync(CancellationToken ct = default) => db.Suppliers
        .AsNoTracking()
        .Include(x => x.ImportReceipts)
        .Include(x => x.SupplierProducts).ThenInclude(x => x.Product)
        .OrderBy(x => x.SupplierID)
        .ToListAsync(ct);

    public Task<List<Supplier>> GetActiveSuppliersAsync(CancellationToken ct = default) => db.Suppliers
        .AsNoTracking()
        .Where(x => x.IsActive)
        .OrderBy(x => x.SupplierName)
        .ToListAsync(ct);

    public Task<Supplier?> GetSupplierDetailsAsync(int supplierId, CancellationToken ct = default) => db.Suppliers
        .AsNoTracking()
        .Include(x => x.SupplierProducts).ThenInclude(x => x.Product)
        .FirstOrDefaultAsync(x => x.SupplierID == supplierId, ct);

    public async Task SaveSupplierAsync(Supplier item, CancellationToken ct = default)
    {
        item.SupplierName = item.SupplierName?.Trim() ?? string.Empty;
        item.Phone = item.Phone?.Trim();
        item.Address = item.Address?.Trim() ?? string.Empty;

        if (item.SupplierName.Length == 0)
            throw new InvalidOperationException("Vui lòng nhập tên nhà cung cấp.");
        if (item.SupplierName.Length > 150)
            throw new InvalidOperationException("Tên nhà cung cấp không được vượt quá 150 ký tự.");
        if (string.IsNullOrWhiteSpace(item.Phone))
            throw new InvalidOperationException("Vui lòng nhập số điện thoại nhà cung cấp.");
        if (item.Phone.Length > 20)
            throw new InvalidOperationException("Số điện thoại không được vượt quá 20 ký tự.");
        if (item.Phone.Any(character => !char.IsAsciiDigit(character) && !"+()-. ".Contains(character)) ||
            item.Phone.Count(char.IsAsciiDigit) is < 8 or > 15)
            throw new InvalidOperationException("Số điện thoại cần có từ 8 đến 15 chữ số; chỉ dùng số và các ký tự + ( ) - hoặc khoảng trắng.");
        if (item.Address.Length == 0)
            throw new InvalidOperationException("Vui lòng nhập địa chỉ nhà cung cấp.");
        if (item.Address?.Length > 255)
            throw new InvalidOperationException("Địa chỉ không được vượt quá 255 ký tự.");

        if (item.SupplierID == 0)
        {
            db.Suppliers.Add(item);
        }
        else
        {
            var current = await db.Suppliers.FindAsync([item.SupplierID], ct) ?? throw new InvalidOperationException("Không tìm thấy nhà cung cấp.");
            current.SupplierName = item.SupplierName;
            current.Phone = item.Phone;
            current.Address = item.Address;
            current.IsActive = item.IsActive;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteSupplierAsync(int supplierId, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers
            .Include(x => x.ImportReceipts)
            .FirstOrDefaultAsync(x => x.SupplierID == supplierId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy nhà cung cấp.");

        if (supplier.ImportReceipts.Count > 0)
            throw new InvalidOperationException("Nhà cung cấp đã có phiếu nhập nên không thể xóa. Hãy ngừng hoạt động để ẩn nhà cung cấp này.");

        db.Suppliers.Remove(supplier);
        await db.SaveChangesAsync(ct);
    }

    public async Task SetSupplierStatusAsync(int supplierId, bool isActive, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers.FindAsync([supplierId], ct)
            ?? throw new InvalidOperationException("Không tìm thấy nhà cung cấp.");

        supplier.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    public Task<List<int>> GetSupplierProductIdsAsync(int supplierId, CancellationToken ct = default) =>
        db.SupplierProducts.AsNoTracking().Where(x => x.SupplierID == supplierId)
            .Select(x => x.ProductID).ToListAsync(ct);

    public Task<List<Product>> GetProductsForSupplierAsync(int supplierId, CancellationToken ct = default) =>
        db.SupplierProducts.AsNoTracking()
            .Where(x => x.SupplierID == supplierId && x.Supplier.IsActive && x.Product.IsActive)
            .Select(x => x.Product).OrderBy(x => x.ProductName).ToListAsync(ct);

    public async Task SaveSupplierProductsAsync(int supplierId, IReadOnlyCollection<int> productIds, CancellationToken ct = default)
    {
        if (!await db.Suppliers.AnyAsync(x => x.SupplierID == supplierId, ct))
            throw new InvalidOperationException("Không tìm thấy nhà cung cấp.");

        var selectedIds = productIds.Distinct().ToArray();
        var activeProductCount = await db.Products.CountAsync(x => selectedIds.Contains(x.ProductID) && x.IsActive, ct);
        if (activeProductCount != selectedIds.Length)
            throw new InvalidOperationException("Một số sản phẩm không tồn tại hoặc đã ngừng kinh doanh.");

        var current = await db.SupplierProducts.Where(x => x.SupplierID == supplierId).ToListAsync(ct);
        var remove = current.Where(x => !selectedIds.Contains(x.ProductID)).ToList();
        if (remove.Count > 0)
            db.SupplierProducts.RemoveRange(remove);

        var currentIds = current.Select(x => x.ProductID).ToHashSet();
        foreach (var productId in selectedIds.Where(id => !currentIds.Contains(id)))
            db.SupplierProducts.Add(new SupplierProduct { SupplierID = supplierId, ProductID = productId });

        await db.SaveChangesAsync(ct);
    }

    public async Task SaveReceiptAsync(ImportReceiptInput input, CancellationToken ct = default)
    {
        var submittedLines = input.Lines ?? [];
        var enteredLines = submittedLines
            .Where(x => x.ProductID != 0 || x.Quantity != 0 || x.UnitPrice != 0)
            .ToList();

        if (input.SupplierID <= 0)
            throw new InvalidOperationException("Vui lòng chọn nhà cung cấp.");
        if (input.EmployeeID <= 0)
            throw new InvalidOperationException("Vui lòng chọn nhân viên lập phiếu.");
        if (enteredLines.Count == 0)
            throw new InvalidOperationException("Vui lòng thêm ít nhất một sản phẩm vào phiếu nhập.");
        if (enteredLines.Any(x => x.ProductID <= 0 || x.Quantity <= 0 || x.UnitPrice < 0))
            throw new InvalidOperationException("Kiểm tra sản phẩm, số lượng phải lớn hơn 0 và đơn giá không được âm.");

        var duplicateProduct = enteredLines
            .GroupBy(x => x.ProductID)
            .FirstOrDefault(group => group.Select(x => x.UnitPrice).Distinct().Count() > 1);
        if (duplicateProduct is not null)
            throw new InvalidOperationException("Một sản phẩm không thể có nhiều đơn giá trong cùng phiếu. Hãy gộp số lượng và chọn một đơn giá.");

        var lines = enteredLines
            .GroupBy(x => x.ProductID)
            .Select(group => new ImportLineInput
            {
                ProductID = group.Key,
                Quantity = group.Sum(x => x.Quantity),
                UnitPrice = group.First().UnitPrice
            })
            .ToList();

        if (!await db.Suppliers.AnyAsync(s => s.SupplierID == input.SupplierID && s.IsActive, ct))
            throw new InvalidOperationException("Nhà cung cấp đã chọn không tồn tại hoặc đã ngừng hoạt động.");

        var employee = await db.Employees
            .Include(e => e.Account)
            .FirstOrDefaultAsync(e => e.EmployeeID == input.EmployeeID, ct);
        if (employee is null || employee.Account.Status != AccountStatus.Active)
            throw new InvalidOperationException("Nhân viên lập phiếu không tồn tại hoặc đã bị khóa.");

        var productIds = lines.Select(x => x.ProductID).ToArray();
        var products = await db.Products
            .Where(p => productIds.Contains(p.ProductID) && p.IsActive)
            .ToDictionaryAsync(p => p.ProductID, ct);
        if (products.Count != lines.Count)
            throw new InvalidOperationException("Một số sản phẩm không tồn tại hoặc đã ngừng kinh doanh.");

        var mappedProductCount = await db.SupplierProducts.CountAsync(
            x => x.SupplierID == input.SupplierID && productIds.Contains(x.ProductID), ct);
        if (mappedProductCount != lines.Count)
            throw new InvalidOperationException("Chỉ được nhập các sản phẩm đã được gán cho nhà cung cấp đang chọn. Hãy kiểm tra danh sách sản phẩm của nhà cung cấp.");

        var receipt = new ImportReceipt { SupplierID = input.SupplierID, EmployeeID = employee.EmployeeID, ReceiptDate = DateTime.Now, Note = input.Note?.Trim(), TotalAmount = lines.Sum(x => x.Quantity * x.UnitPrice) };
        foreach (var line in lines) { products[line.ProductID].Quantity = checked(products[line.ProductID].Quantity + line.Quantity); receipt.ImportReceiptDetails.Add(new ImportReceiptDetail { ProductID = line.ProductID, Quantity = line.Quantity, UnitPrice = line.UnitPrice, SubTotal = line.Quantity * line.UnitPrice }); }
        db.ImportReceipts.Add(receipt);
        await db.SaveChangesAsync(ct);
    }
    public Task<List<ImportReceipt>> GetReceiptsAsync(CancellationToken ct = default) => db.ImportReceipts.AsNoTracking().Include(x => x.Supplier).Include(x => x.Employee).Include(x => x.ImportReceiptDetails).ThenInclude(x => x.Product).OrderByDescending(x => x.ReceiptDate).ToListAsync(ct);
    public Task<List<ImportReceipt>> GetReceiptsBySupplierAsync(int supplierId, CancellationToken ct = default) => db.ImportReceipts
        .AsNoTracking()
        .Where(x => x.SupplierID == supplierId)
        .Include(x => x.Employee)
        .Include(x => x.ImportReceiptDetails).ThenInclude(x => x.Product)
        .OrderByDescending(x => x.ReceiptDate)
        .ThenByDescending(x => x.ReceiptID)
        .ToListAsync(ct);
    public Task<List<Product>> GetProductsAsync(CancellationToken ct = default) => db.Products.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.ProductName).ToListAsync(ct);
}

public sealed class PromotionService(IBakeryDbContext db)
{
    public Task<List<Promotion>> GetAsync(CancellationToken ct = default) => db.Promotions.AsNoTracking()
        .Include(p => p.PromotionInvoice)
        .Include(p => p.PromotionOrderTier)
        .Include(p => p.PromotionProducts).ThenInclude(x => x.Product)
        .OrderByDescending(p => p.StartDate).ToListAsync(ct);

    public Task<Promotion?> GetByIdAsync(int promotionId, CancellationToken ct = default) => db.Promotions
        .AsNoTracking()
        .Include(p => p.PromotionInvoice)
        .Include(p => p.PromotionOrderTier)
        .Include(p => p.PromotionProducts).ThenInclude(x => x.Product)
        .FirstOrDefaultAsync(p => p.PromotionID == promotionId, ct);

    public Task<List<Order>> GetOrdersUsingAsync(int promotionId, CancellationToken ct = default) => db.Orders
        .AsNoTracking()
        .Include(o => o.Customer)
        .Where(o => o.PromotionID == promotionId)
        .OrderByDescending(o => o.OrderDate)
        .ToListAsync(ct);

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
            (useInvoiceCondition && (invoicePercent <= 0 || invoicePercent > 100 || minimum < 0 || invoiceCap is null || invoiceCap <= 0)) ||
            (useProductCondition && (productIds.Count == 0 || productPercent <= 0 || productPercent > 100 || productCap is null || productCap <= 0)))
            throw new InvalidOperationException("Chọn ít nhất một điều kiện; phần trăm, đơn tối thiểu và mức giảm tối đa phải hợp lệ. Mức giảm tối đa phải lớn hơn 0.");

        if (useProductCondition)
        {
            var selectedIds = productIds.Distinct().ToArray();
            var activeProductCount = await db.Products
                .CountAsync(p => selectedIds.Contains(p.ProductID) && p.IsActive, ct);
            if (activeProductCount != selectedIds.Length)
                throw new InvalidOperationException("Một số sản phẩm đã chọn không tồn tại hoặc đã ngừng kinh doanh.");
        }
        if (await db.Promotions.AnyAsync(p => p.PromotionCode == campaign.PromotionCode, ct)) throw new InvalidOperationException("Mã chương trình đã tồn tại.");
        campaign.PromotionType = (useInvoiceCondition, useProductCondition) switch
        {
            (true, true) => "InvoiceProduct",
            (true, false) => "Invoice",
            _ => "Product"
        };
        campaign.Status = PromotionStatus.Draft;
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

    public async Task<HashSet<int>> GetUsedPromotionIdsAsync(CancellationToken ct = default) =>
        (await db.Orders.AsNoTracking()
            .Where(o => o.PromotionID.HasValue)
            .Select(o => o.PromotionID!.Value)
            .Distinct()
            .ToListAsync(ct))
        .ToHashSet();

    public async Task DeleteUnusedAsync(int promotionId, CancellationToken ct = default)
    {
        var promotion = await db.Promotions.FirstOrDefaultAsync(p => p.PromotionID == promotionId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy chương trình khuyến mãi.");
        if (await HasBeenUsedAsync(promotionId, ct))
            throw new InvalidOperationException("Khuyến mãi đã được dùng trong đơn hàng nên không thể xóa. Hãy giữ hồ sơ để bảo toàn lịch sử.");
        if (promotion.Status == PromotionStatus.Active && promotion.EndDate.Date >= DateTime.Today)
            throw new InvalidOperationException("Khuyến mãi còn hạn và đang hoạt động. Hãy tạm dừng trước khi xóa.");
        db.Promotions.Remove(promotion);
        await db.SaveChangesAsync(ct);
    }

    public async Task SetStatusAsync(int promotionId, string status, CancellationToken ct = default)
    {
        var promotion = await db.Promotions.FindAsync([promotionId], ct)
            ?? throw new InvalidOperationException("Không tìm thấy chương trình khuyến mãi.");
        if (status is not (PromotionStatus.Active or PromotionStatus.Hidden or PromotionStatus.Draft))
            throw new InvalidOperationException("Trạng thái chương trình không hợp lệ.");
        if (status == PromotionStatus.Draft && await HasBeenUsedAsync(promotionId, ct))
            throw new InvalidOperationException("Khuyến mãi đã được dùng trong đơn hàng, không thể chuyển về nháp.");
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
                useInvoiceCondition && (invoicePercent <= 0 || invoicePercent > 100 || minimum < 0 || invoiceCap is null || invoiceCap <= 0) ||
                useProductCondition && (productIds.Count == 0 || productPercent <= 0 || productPercent > 100 || productCap is null || productCap <= 0))
                throw new InvalidOperationException("Chọn ít nhất một điều kiện; phần trăm, đơn tối thiểu và mức giảm tối đa phải hợp lệ. Mức giảm tối đa phải lớn hơn 0.");

            if (useProductCondition)
            {
                var selectedIds = productIds.Distinct().ToArray();
                var activeProductCount = await db.Products
                    .CountAsync(p => selectedIds.Contains(p.ProductID) && p.IsActive, ct);
                if (activeProductCount != selectedIds.Length)
                    throw new InvalidOperationException("Một số sản phẩm đã chọn không tồn tại hoặc đã ngừng kinh doanh.");
            }

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
