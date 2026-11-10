using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Domain.Entities;
using BakerySystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.Application.Features.Inventory;

public sealed class ImportLineInput { public int ProductID { get; set; } public int Quantity { get; set; } public decimal UnitPrice { get; set; } }
public sealed class ImportReceiptInput
{
    public int SupplierID { get; set; }
    public int EmployeeID { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = ImportReceiptStatus.Completed;
    public List<ImportLineInput> Lines { get; set; } = [];
}
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

    public Task<Dictionary<int, decimal>> GetSupplierProductPricesAsync(int supplierId, CancellationToken ct = default) =>
        db.SupplierProducts.AsNoTracking().Where(x => x.SupplierID == supplierId)
            .ToDictionaryAsync(x => x.ProductID, x => x.PurchasePrice, ct);

    public async Task SaveSupplierProductPriceAsync(int supplierId, int productId, decimal purchasePrice, CancellationToken ct = default)
    {
        if (purchasePrice <= 0)
            throw new InvalidOperationException("Giá vốn phải lớn hơn 0.");

        var link = await db.SupplierProducts.FirstOrDefaultAsync(
            x => x.SupplierID == supplierId && x.ProductID == productId, ct)
            ?? throw new InvalidOperationException("Sản phẩm chưa được gán cho nhà cung cấp.");
        link.PurchasePrice = purchasePrice;
        await db.SaveChangesAsync(ct);
    }

    public async Task<Dictionary<int, int>> GetProductSupplierIdsAsync(CancellationToken ct = default) =>
        await db.SupplierProducts.AsNoTracking()
            .ToDictionaryAsync(x => x.ProductID, x => x.SupplierID, ct);

    public Task<List<Product>> GetProductsForSupplierAsync(int supplierId, CancellationToken ct = default) =>
        db.SupplierProducts.AsNoTracking()
            .Where(x => x.SupplierID == supplierId && x.Supplier.IsActive && x.Product.IsActive)
            .Select(x => x.Product).OrderBy(x => x.ProductName).ToListAsync(ct);

    public async Task SaveSupplierProductsAsync(
        int supplierId,
        IReadOnlyCollection<int> productIds,
        IReadOnlyDictionary<int, decimal> purchasePrices,
        CancellationToken ct = default)
    {
        if (!await db.Suppliers.AnyAsync(x => x.SupplierID == supplierId, ct))
            throw new InvalidOperationException("Không tìm thấy nhà cung cấp.");

        var selectedIds = productIds.Distinct().ToArray();
        var activeProductCount = await db.Products.CountAsync(x => selectedIds.Contains(x.ProductID) && x.IsActive, ct);
        if (activeProductCount != selectedIds.Length)
            throw new InvalidOperationException("Một số sản phẩm không tồn tại hoặc đã ngừng kinh doanh.");

        var conflictingProduct = await db.SupplierProducts
            .Where(x => selectedIds.Contains(x.ProductID) && x.SupplierID != supplierId)
            .Select(x => new { x.Product.ProductName, x.Supplier.SupplierName })
            .FirstOrDefaultAsync(ct);
        if (conflictingProduct is not null)
            throw new InvalidOperationException(
                $"Sản phẩm {conflictingProduct.ProductName} đã thuộc nhà cung cấp {conflictingProduct.SupplierName}. Hãy bỏ gán ở nhà cung cấp đó trước.");

        var current = await db.SupplierProducts.Where(x => x.SupplierID == supplierId).ToListAsync(ct);
        var currentByProductId = current.ToDictionary(x => x.ProductID);
        var selectedPrices = new Dictionary<int, decimal>();
        foreach (var productId in selectedIds)
        {
            var price = purchasePrices.TryGetValue(productId, out var enteredPrice)
                ? enteredPrice
                : currentByProductId.GetValueOrDefault(productId)?.PurchasePrice ?? 0;
            if (price <= 0)
                throw new InvalidOperationException("Vui lòng cập nhật giá vốn lớn hơn 0 cho tất cả bánh được chọn trước khi lưu.");
            selectedPrices[productId] = price;
        }

        var remove = current.Where(x => !selectedIds.Contains(x.ProductID)).ToList();
        if (remove.Count > 0)
            db.SupplierProducts.RemoveRange(remove);

        foreach (var productId in selectedIds)
        {
            if (currentByProductId.TryGetValue(productId, out var link))
                link.PurchasePrice = selectedPrices[productId];
            else
                db.SupplierProducts.Add(new SupplierProduct
                {
                    SupplierID = supplierId,
                    ProductID = productId,
                    PurchasePrice = selectedPrices[productId]
                });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<int> SaveReceiptAsync(ImportReceiptInput input, CancellationToken ct = default)
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
        if (enteredLines.Any(x => x.ProductID <= 0 || x.Quantity <= 0 || x.UnitPrice <= 0))
            throw new InvalidOperationException("Kiểm tra sản phẩm, số lượng và đơn giá vốn: tất cả phải lớn hơn 0.");

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

        var supplierProductLinks = await db.SupplierProducts
            .Where(x => x.SupplierID == input.SupplierID && productIds.Contains(x.ProductID))
            .ToListAsync(ct);
        if (supplierProductLinks.Count != lines.Count)
            throw new InvalidOperationException("Chỉ được nhập các sản phẩm đã được gán cho nhà cung cấp đang chọn. Hãy kiểm tra danh sách sản phẩm của nhà cung cấp.");

        var linePrices = lines.ToDictionary(x => x.ProductID, x => x.UnitPrice);
        foreach (var link in supplierProductLinks)
            link.PurchasePrice = linePrices[link.ProductID];

        var status = input.Status is ImportReceiptStatus.Pending or ImportReceiptStatus.Completed
            ? input.Status
            : ImportReceiptStatus.Completed;

        var receipt = new ImportReceipt
        {
            SupplierID = input.SupplierID,
            EmployeeID = employee.EmployeeID,
            ReceiptDate = DateTime.Now,
            Status = status,
            Note = input.Note?.Trim(),
            TotalAmount = lines.Sum(x => x.Quantity * x.UnitPrice)
        };

        foreach (var line in lines)
        {
            if (status == ImportReceiptStatus.Completed)
            {
                products[line.ProductID].Quantity = checked(products[line.ProductID].Quantity + line.Quantity);
            }

            receipt.ImportReceiptDetails.Add(new ImportReceiptDetail
            {
                ProductID = line.ProductID,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                SubTotal = line.Quantity * line.UnitPrice
            });
        }

        db.ImportReceipts.Add(receipt);
        await db.SaveChangesAsync(ct);
        return receipt.ReceiptID;
    }

    public async Task CompleteReceiptAsync(int receiptId, CancellationToken ct = default)
    {
        var receipt = await db.ImportReceipts
            .Include(r => r.ImportReceiptDetails)
            .FirstOrDefaultAsync(r => r.ReceiptID == receiptId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy phiếu nhập kho.");

        if (receipt.Status != ImportReceiptStatus.Pending)
            throw new InvalidOperationException($"Không thể xác nhận phiếu nhập ở trạng thái '{ImportReceiptStatus.GetLabel(receipt.Status)}'. Chỉ áp dụng cho phiếu Chờ nhập kho.");

        var productIds = receipt.ImportReceiptDetails.Select(d => d.ProductID).Distinct().ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.ProductID))
            .ToDictionaryAsync(p => p.ProductID, ct);

        var links = await db.SupplierProducts
            .Where(sp => sp.SupplierID == receipt.SupplierID && productIds.Contains(sp.ProductID))
            .ToDictionaryAsync(sp => sp.ProductID, ct);

        foreach (var detail in receipt.ImportReceiptDetails)
        {
            if (products.TryGetValue(detail.ProductID, out var product))
            {
                product.Quantity = checked(product.Quantity + detail.Quantity);
            }
            if (links.TryGetValue(detail.ProductID, out var link))
            {
                link.PurchasePrice = detail.UnitPrice;
            }
        }

        receipt.Status = ImportReceiptStatus.Completed;
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelReceiptAsync(int receiptId, string? cancelReason = null, CancellationToken ct = default)
    {
        var receipt = await db.ImportReceipts
            .Include(r => r.ImportReceiptDetails)
            .FirstOrDefaultAsync(r => r.ReceiptID == receiptId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy phiếu nhập kho.");

        if (receipt.Status == ImportReceiptStatus.Cancelled)
            throw new InvalidOperationException("Phiếu nhập này đã bị hủy trước đó.");

        if (receipt.Status == ImportReceiptStatus.Completed)
        {
            var productIds = receipt.ImportReceiptDetails.Select(d => d.ProductID).Distinct().ToList();
            var products = await db.Products
                .Where(p => productIds.Contains(p.ProductID))
                .ToDictionaryAsync(p => p.ProductID, ct);

            // Kiểm tra số lượng tồn kho có đủ để hoàn tác không
            foreach (var detail in receipt.ImportReceiptDetails)
            {
                if (products.TryGetValue(detail.ProductID, out var product))
                {
                    if (product.Quantity < detail.Quantity)
                    {
                        throw new InvalidOperationException(
                            $"Không thể hoàn tác phiếu nhập vì sản phẩm '{product.ProductName}' hiện chỉ còn tồn {product.Quantity} cái trong kho (ít hơn {detail.Quantity} cái đã nhập), hàng có thể đã được xuất bán.");
                    }
                }
            }

            // Hoàn tác trừ lại tồn kho
            foreach (var detail in receipt.ImportReceiptDetails)
            {
                if (products.TryGetValue(detail.ProductID, out var product))
                {
                    product.Quantity -= detail.Quantity;
                }
            }
        }

        receipt.Status = ImportReceiptStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(cancelReason))
        {
            receipt.Note = string.IsNullOrWhiteSpace(receipt.Note)
                ? $"[Đã hủy: {cancelReason.Trim()}]"
                : $"{receipt.Note.Trim()} | [Đã hủy: {cancelReason.Trim()}]";
        }

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
    public Task<List<Product>> GetProductsAsync(CancellationToken ct = default) => db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.IsActive).OrderBy(p => p.ProductName).ToListAsync(ct);
    public Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default) => db.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync(ct);
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
        int? usageLimit = null,
        decimal? invoiceDiscountAmount = null,
        CancellationToken ct = default)
    {
        campaign.PromotionCode = campaign.PromotionCode.Trim().ToUpperInvariant();
        if (campaign.PromotionCode.Length == 0 || campaign.StartDate.Date > campaign.EndDate.Date)
            throw new InvalidOperationException("Vui lòng nhập mã khuyến mãi hợp lệ và ngày bắt đầu không lớn hơn ngày kết thúc.");

        var hasValidInvoice = useInvoiceCondition && (
            (invoiceDiscountAmount is > 0 && minimum >= 0) ||
            (invoicePercent > 0 && invoicePercent <= 100 && minimum >= 0 && invoiceCap is > 0)
        );
        var hasValidProduct = useProductCondition && (
            productIds.Count > 0 && productPercent > 0 && productPercent <= 100 && productCap is > 0
        );

        if (!hasValidInvoice && !hasValidProduct)
            throw new InvalidOperationException("Vui lòng chọn điều kiện áp dụng hợp lệ: tỷ lệ phần trăm (1-100%), số tiền giảm hoặc mức giảm tối đa phải lớn hơn 0.");

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
        campaign.Status = PromotionStatus.Active;
        db.Promotions.Add(campaign);
        if (useInvoiceCondition)
            campaign.PromotionInvoice = new PromotionInvoice
            {
                Promotion = campaign,
                VoucherCode = campaign.PromotionCode,
                DiscountPercent = invoiceDiscountAmount is > 0 ? 0 : invoicePercent,
                DiscountAmount = invoiceDiscountAmount is > 0 ? invoiceDiscountAmount : null,
                MinOrderAmount = minimum,
                MaxDiscountAmount = invoiceDiscountAmount is > 0 ? null : invoiceCap,
                UsageLimit = usageLimit
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

    public async Task SyncUsedCountAsync(int promotionId, int count, CancellationToken ct = default)
    {
        var invoice = await db.PromotionInvoices.FirstOrDefaultAsync(pi => pi.PromotionID == promotionId, ct);
        if (invoice != null && invoice.UsedCount != count)
        {
            invoice.UsedCount = count;
            await db.SaveChangesAsync(ct);
        }
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
        if (status is not (PromotionStatus.Active or PromotionStatus.Inactive or PromotionStatus.Draft))
            throw new InvalidOperationException("Trạng thái chương trình không hợp lệ.");
        if (status == PromotionStatus.Draft && await HasBeenUsedAsync(promotionId, ct))
            throw new InvalidOperationException("Khuyến mãi đã được dùng trong đơn hàng, không thể chuyển về nháp.");

        var updated = await db.Promotions
            .Where(p => p.PromotionID == promotionId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Status, status), ct);
        if (updated == 0)
            throw new InvalidOperationException("Không tìm thấy chương trình khuyến mãi.");
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
        int? usageLimit = null,
        decimal? invoiceDiscountAmount = null,
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
            var hasValidInvoice = useInvoiceCondition && (
                (invoiceDiscountAmount is > 0 && minimum >= 0) ||
                (invoicePercent > 0 && invoicePercent <= 100 && minimum >= 0 && invoiceCap is > 0)
            );
            var hasValidProduct = useProductCondition && (
                productIds.Count > 0 && productPercent > 0 && productPercent <= 100 && productCap is > 0
            );

            if (!hasValidInvoice && !hasValidProduct)
                throw new InvalidOperationException("Vui lòng chọn điều kiện áp dụng hợp lệ: tỷ lệ phần trăm (1-100%), số tiền giảm hoặc mức giảm tối đa phải lớn hơn 0.");

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
                    DiscountPercent = invoiceDiscountAmount is > 0 ? 0 : invoicePercent,
                    DiscountAmount = invoiceDiscountAmount is > 0 ? invoiceDiscountAmount : null,
                    MinOrderAmount = minimum,
                    MaxDiscountAmount = invoiceDiscountAmount is > 0 ? null : invoiceCap,
                    UsageLimit = usageLimit
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
        else
        {
            if (current.PromotionInvoice is not null)
                current.PromotionInvoice.UsageLimit = usageLimit;

            if (originalCode != campaign.PromotionCode)
                throw new InvalidOperationException("Không thể đổi mã hoặc điều kiện của chương trình đã có đơn hàng sử dụng.");
        }

        await db.SaveChangesAsync(ct);
    }
}
