using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Domain.Entities;
using BakerySystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.Application.Features.Reports;

public sealed class ReportQueryResult
{
    public List<Order> Orders { get; init; } = [];
    public List<ImportReceipt> Imports { get; init; } = [];
    public List<OrderDetail> OrderDetails { get; init; } = [];
    public List<Product> LowStockProducts { get; init; } = [];
    public int SuppliersCount { get; init; }
}

public sealed class ReportAnalyticsRequest
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public DateTime? PreviousStartDate { get; init; }
    public DateTime? PreviousEndDate { get; init; }
    public string TimelineMode { get; init; } = "month"; // "hours" | "days" | "weeks" | "months"
    public int ReferenceYear { get; init; } = DateTime.Now.Year;
    public int ReferenceMonth { get; init; } = DateTime.Now.Month;
    public int ReferenceQuarter { get; init; } = (DateTime.Now.Month - 1) / 3 + 1;
}

public sealed class ReportAnalyticsResult
{
    public ReportKpiSummary Kpi { get; init; } = new();
    public ReportInventorySummary Inventory { get; init; } = new();
    public List<ReportTimelineItem> Timeline { get; init; } = [];
    public List<ReportChannelItem> Channels { get; init; } = [];
    public List<ReportPaymentItem> Payments { get; init; } = [];
    public List<ReportCategoryItem> Categories { get; init; } = [];
    public List<ReportProductItem> Products { get; init; } = [];
    public List<ReportCustomerItem> TopCustomers { get; init; } = [];
    public List<ReportOrderItem> Orders { get; init; } = [];
    public List<ReportImportItem> Imports { get; init; } = [];
}

public sealed class ReportKpiSummary
{
    public decimal ActualRevenue { get; init; }
    public decimal CostOfGoodsSold { get; init; }
    public decimal GrossProfit => ActualRevenue - CostOfGoodsSold;
    public decimal ProfitMargin => ActualRevenue > 0 ? (GrossProfit / ActualRevenue) * 100m : 0m;
    public decimal VoucherDiscount { get; init; }
    public decimal AverageOrderValue => CompletedOrdersCount > 0 ? ActualRevenue / CompletedOrdersCount : 0m;

    public int TotalOrdersCount { get; init; }
    public int CompletedOrdersCount { get; init; }
    public int ProcessingOrdersCount { get; init; }
    public decimal ProcessingOrdersAmount { get; init; }
    public int CancelledOrdersCount { get; init; }
    public decimal CancelledOrdersAmount { get; init; }
    public decimal OrderSuccessRate => TotalOrdersCount > 0 ? (decimal)CompletedOrdersCount / TotalOrdersCount * 100m : 0m;
    public decimal CancellationRate => TotalOrdersCount > 0 ? (decimal)CancelledOrdersCount / TotalOrdersCount * 100m : 0m;

    public decimal PeriodImportCashflow { get; init; }
    public int ImportReceiptsCount { get; init; }
    public int SuppliersCount { get; init; }

    public bool HasComparison { get; init; }
    public decimal? RevenueGrowthPercent { get; init; }
    public decimal? GrossProfitGrowthPercent { get; init; }
    public decimal? OrdersGrowthPercent { get; init; }
    public decimal? AovGrowthPercent { get; init; }
    public decimal PreviousRevenue { get; init; }
    public decimal PreviousGrossProfit { get; init; }
    public int PreviousCompletedOrdersCount { get; init; }
    public decimal PreviousAov { get; init; }
}

public sealed class ReportInventorySummary
{
    public int ActiveProductsCount { get; init; }
    public int TotalStockUnits { get; init; }
    public decimal TotalInventoryCostValue { get; init; }
    public decimal TotalInventoryRetailValue { get; init; }
    public int OutOfStockCount { get; init; }
    public int LowStockCount { get; init; }
    public List<ReportInventoryItem> AllItems { get; init; } = [];
    public List<ReportInventoryItem> LowStockItems { get; init; } = [];
}

public sealed class ReportInventoryItem
{
    public int ProductID { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string CategoryName { get; init; } = "Khác";
    public int StockQuantity { get; init; }
    public decimal RetailPrice { get; init; }
    public decimal UnitCost { get; init; }
    public decimal InventoryCostValue => StockQuantity * UnitCost;
    public decimal InventoryRetailValue => StockQuantity * RetailPrice;
    public bool HasUnitCost { get; init; }
}

public sealed class ReportTimelineItem
{
    public string BucketKey { get; init; } = string.Empty;
    public string TimeLabel { get; init; } = string.Empty;
    public string ShortLabel { get; init; } = string.Empty;
    public int CompletedOrderCount { get; init; }
    public int CancelledOrderCount { get; init; }
    public decimal Revenue { get; init; }
    public decimal CostOfGoodsSold { get; init; }
    public decimal GrossProfit => Revenue - CostOfGoodsSold;
    public decimal Margin => Revenue > 0 ? (GrossProfit / Revenue) * 100m : 0m;
    public decimal ImportCashflow { get; init; }
    public int ImportReceiptsCount { get; init; }
}

public sealed class ReportChannelItem
{
    public string ChannelKey { get; init; } = string.Empty; // "pos" | "online"
    public string DisplayName { get; init; } = string.Empty;
    public int OrderCount { get; init; }
    public decimal Revenue { get; init; }
    public decimal CostOfGoodsSold { get; init; }
    public decimal GrossProfit => Revenue - CostOfGoodsSold;
    public decimal Percentage { get; init; }
    public string Icon { get; init; } = string.Empty;
}

public sealed class ReportPaymentItem
{
    public string GroupKey { get; init; } = string.Empty; // "cod_cash" | "bank_transfer" | "vnpay" | "momo" | "other"
    public string DisplayName { get; init; } = string.Empty;
    public int OrderCount { get; init; }
    public decimal Amount { get; init; }
    public decimal Percentage { get; init; }
    public string Icon { get; init; } = string.Empty;
    public string BadgeTone { get; init; } = "blue";
}

public sealed class ReportCategoryItem
{
    public int CategoryID { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int ProductCount { get; init; }
    public int SoldQuantity { get; init; }
    public decimal Revenue { get; init; }
    public decimal CostOfGoodsSold { get; init; }
    public decimal GrossProfit => Revenue - CostOfGoodsSold;
    public decimal Margin => Revenue > 0 ? (GrossProfit / Revenue) * 100m : 0m;
    public decimal Percentage { get; init; }
    public string ColorHex { get; init; } = "#2563eb";
}

public sealed class ReportProductItem
{
    public int ProductID { get; init; }
    public int CategoryID { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string CategoryName { get; init; } = "Khác";
    public decimal RetailPrice { get; init; }
    public decimal UnitCost { get; init; }
    public bool HasUnitCost { get; init; }
    public int CurrentStock { get; init; }
    public int SoldQuantity { get; init; }
    public int OrderCount { get; init; }
    public decimal GrossSalesBeforeVoucher { get; init; }
    public decimal NetRevenue { get; init; }
    public decimal TotalCost { get; init; }
    public decimal GrossProfit => NetRevenue - TotalCost;
    public decimal Margin => NetRevenue > 0 ? (GrossProfit / NetRevenue) * 100m : 0m;
}

public sealed class ReportCustomerItem
{
    public int CustomerID { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public int CompletedOrderCount { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal AverageOrderValue => CompletedOrderCount > 0 ? TotalSpent / CompletedOrderCount : 0m;
    public DateTime LastOrderDate { get; init; }
}

public sealed class ReportOrderItem
{
    public int OrderID { get; init; }
    public int CustomerID { get; init; }
    public DateTime OrderDate { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string? EmployeeName { get; init; }
    public string ChannelKey { get; init; } = "pos";
    public string ChannelLabel { get; init; } = string.Empty;
    public string? ShippingAddress { get; init; }
    public string PaymentGroupKey { get; init; } = string.Empty;
    public string PaymentMethodLabel { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string StatusClass { get; init; } = string.Empty;
    public string StatusGroup { get; init; } = string.Empty; // "completed" | "processing" | "cancelled"
    public decimal TotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public decimal CostOfGoodsSold { get; init; }
    public decimal GrossProfit => Status == OrderStatus.Completed ? FinalAmount - CostOfGoodsSold : 0m;
    public string TimelineBucketKey { get; init; } = string.Empty;
    public List<ReportOrderLineItem> Lines { get; init; } = [];
}

public sealed class ReportOrderLineItem
{
    public int ProductID { get; init; }
    public int CategoryID { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string CategoryName { get; init; } = "Khác";
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal SubTotal { get; init; }
    public decimal AllocatedNetRevenue { get; init; }
    public decimal LineCost { get; init; }
}

public sealed class ReportImportItem
{
    public int ReceiptID { get; init; }
    public DateTime ReceiptDate { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string? Note { get; init; }
    public string TimelineBucketKey { get; init; } = string.Empty;
    public List<ReportImportLineItem> Lines { get; init; } = [];
}

public sealed class ReportImportLineItem
{
    public int ProductID { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal SubTotal { get; init; }
}

public sealed class ReportService(IBakeryDbContext db)
{
    private static readonly string[] CategoryPalette =
    [
        "#2563eb", "#10b981", "#f59e0b", "#8b5cf6", "#ec4899", "#06b6d4", "#f97316"
    ];

    public async Task<ReportQueryResult> GetReportDataAsync(
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default)
    {
        var ordersQuery = db.Orders
            .Include(o => o.Customer)
            .AsQueryable();
        var importsQuery = db.ImportReceipts
            .Where(i => i.Status == ImportReceiptStatus.Completed)
            .AsQueryable();

        if (startDate.HasValue && endDate.HasValue)
        {
            ordersQuery = ordersQuery.Where(o =>
                o.OrderDate >= startDate.Value && o.OrderDate <= endDate.Value);
            importsQuery = importsQuery.Where(i =>
                i.ReceiptDate >= startDate.Value && i.ReceiptDate <= endDate.Value);
        }

        var orders = await ordersQuery.ToListAsync(ct);
        var imports = await importsQuery.ToListAsync(ct);
        var validOrderIds = orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .Select(o => o.OrderID)
            .ToList();

        var orderDetails = await db.OrderDetails
            .Include(d => d.Product)
                .ThenInclude(p => p.Category)
            .Where(d => validOrderIds.Contains(d.OrderID))
            .ToListAsync(ct);

        var lowStockProducts = await db.Products
            .Include(p => p.Category)
            .Where(p => p.Quantity <= 10 && p.IsActive)
            .OrderBy(p => p.Quantity)
            .ToListAsync(ct);

        return new ReportQueryResult
        {
            Orders = orders,
            Imports = imports,
            OrderDetails = orderDetails,
            LowStockProducts = lowStockProducts,
            SuppliersCount = await db.Suppliers.CountAsync(ct)
        };
    }

    public async Task<ReportAnalyticsResult> GetReportAnalyticsAsync(
        ReportAnalyticsRequest request,
        CancellationToken ct = default)
    {
        var ordersQuery = db.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.Category)
            .AsQueryable();

        var importsQuery = db.ImportReceipts
            .AsNoTracking()
            .Include(i => i.Supplier)
            .Include(i => i.Employee)
            .Include(i => i.ImportReceiptDetails)
                .ThenInclude(d => d.Product)
            .Where(i => i.Status == ImportReceiptStatus.Completed)
            .AsQueryable();

        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            var start = request.StartDate.Value;
            var end = request.EndDate.Value;
            ordersQuery = ordersQuery.Where(o => o.OrderDate >= start && o.OrderDate <= end);
            importsQuery = importsQuery.Where(i => i.ReceiptDate >= start && i.ReceiptDate <= end);
        }

        var orders = await ordersQuery.OrderByDescending(o => o.OrderDate).ToListAsync(ct);
        var imports = await importsQuery.OrderByDescending(i => i.ReceiptDate).ToListAsync(ct);

        var activeProducts = await db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderBy(p => p.ProductName)
            .ToListAsync(ct);

        var suppliersCount = await db.Suppliers.AsNoTracking().CountAsync(ct);

        // Compute authoritative unit COGS per product:
        // 1. Weighted average from completed ImportReceiptDetails (using SubTotal/Qty or UnitPrice)
        // 2. Fallback to SupplierProduct.PurchasePrice
        var unitCostMap = await BuildProductUnitCostMapAsync(request.EndDate, ct);

        // Resolve timeline slots once so orders & imports both snap to the same BucketKey
        var timelineSlots = CreateTimelineSlots(request, orders, imports);

        var orderItems = orders
            .Select(o => MapOrderItem(o, unitCostMap, timelineSlots))
            .ToList();

        var importItems = imports
            .Select(i => MapImportItem(i, timelineSlots))
            .ToList();

        var completedOrders = orderItems.Where(o => o.Status == OrderStatus.Completed).ToList();
        var processingOrders = orderItems.Where(o => o.StatusGroup == "processing").ToList();
        var cancelledOrders = orderItems.Where(o => o.StatusGroup == "cancelled").ToList();

        var actualRevenue = completedOrders.Sum(o => o.FinalAmount);
        var totalCogs = completedOrders.Sum(o => o.CostOfGoodsSold);
        var voucherDiscount = completedOrders.Sum(o => o.DiscountAmount);
        var importCashflow = importItems.Sum(i => i.TotalAmount);

        // Previous period comparison (Completed orders only)
        var hasComparison = request.PreviousStartDate.HasValue && request.PreviousEndDate.HasValue;
        decimal prevRevenue = 0m;
        decimal prevCogs = 0m;
        int prevCompletedCount = 0;

        if (hasComparison)
        {
            var prevStart = request.PreviousStartDate!.Value;
            var prevEnd = request.PreviousEndDate!.Value;

            var prevOrders = await db.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                .Where(o => o.Status == OrderStatus.Completed && o.OrderDate >= prevStart && o.OrderDate <= prevEnd)
                .ToListAsync(ct);

            prevCompletedCount = prevOrders.Count;
            prevRevenue = prevOrders.Sum(o => o.FinalAmount);
            prevCogs = prevOrders.Sum(o => o.OrderDetails.Sum(d =>
                d.Quantity * (unitCostMap.TryGetValue(d.ProductID, out var cost) ? cost : 0m)));
        }

        var prevGrossProfit = prevRevenue - prevCogs;
        var currentAov = completedOrders.Count > 0 ? actualRevenue / completedOrders.Count : 0m;
        var prevAov = prevCompletedCount > 0 ? prevRevenue / prevCompletedCount : 0m;

        var kpi = new ReportKpiSummary
        {
            ActualRevenue = actualRevenue,
            CostOfGoodsSold = totalCogs,
            VoucherDiscount = voucherDiscount,
            TotalOrdersCount = orderItems.Count,
            CompletedOrdersCount = completedOrders.Count,
            ProcessingOrdersCount = processingOrders.Count,
            ProcessingOrdersAmount = processingOrders.Sum(o => o.FinalAmount),
            CancelledOrdersCount = cancelledOrders.Count,
            CancelledOrdersAmount = cancelledOrders.Sum(o => o.FinalAmount),
            PeriodImportCashflow = importCashflow,
            ImportReceiptsCount = importItems.Count,
            SuppliersCount = suppliersCount,
            HasComparison = hasComparison,
            PreviousRevenue = prevRevenue,
            PreviousGrossProfit = prevGrossProfit,
            PreviousCompletedOrdersCount = prevCompletedCount,
            PreviousAov = prevAov,
            RevenueGrowthPercent = hasComparison ? ComputeGrowthPercent(actualRevenue, prevRevenue) : null,
            GrossProfitGrowthPercent = hasComparison ? ComputeGrowthPercent(actualRevenue - totalCogs, prevGrossProfit) : null,
            OrdersGrowthPercent = hasComparison ? ComputeGrowthPercent(completedOrders.Count, prevCompletedCount) : null,
            AovGrowthPercent = hasComparison ? ComputeGrowthPercent(currentAov, prevAov) : null
        };

        var inventory = BuildInventorySummary(activeProducts, unitCostMap);
        var timeline = BuildTimelineItems(timelineSlots, orderItems, importItems);
        var channels = BuildChannelBreakdown(completedOrders, actualRevenue);
        var payments = BuildPaymentBreakdown(completedOrders, actualRevenue);
        var categories = BuildCategoryBreakdown(completedOrders, actualRevenue);
        var products = BuildProductPerformance(activeProducts, completedOrders, unitCostMap);
        var topCustomers = BuildTopCustomers(orders.Where(o => o.Status == OrderStatus.Completed));

        return new ReportAnalyticsResult
        {
            Kpi = kpi,
            Inventory = inventory,
            Timeline = timeline,
            Channels = channels,
            Payments = payments,
            Categories = categories,
            Products = products,
            TopCustomers = topCustomers,
            Orders = orderItems,
            Imports = importItems
        };
    }

    private async Task<Dictionary<int, decimal>> BuildProductUnitCostMapAsync(DateTime? asOfEndDate, CancellationToken ct)
    {
        var importDetailsQuery = db.ImportReceiptDetails
            .AsNoTracking()
            .Where(d => d.ImportReceipt.Status == ImportReceiptStatus.Completed);

        if (asOfEndDate.HasValue)
        {
            var end = asOfEndDate.Value;
            importDetailsQuery = importDetailsQuery.Where(d => d.ImportReceipt.ReceiptDate <= end);
        }

        var importStats = await importDetailsQuery
            .GroupBy(d => d.ProductID)
            .Select(g => new
            {
                ProductID = g.Key,
                TotalQty = g.Sum(x => x.Quantity),
                TotalSubTotal = g.Sum(x => x.SubTotal),
                AvgUnitPrice = g.Average(x => x.UnitPrice)
            })
            .ToListAsync(ct);

        var costMap = new Dictionary<int, decimal>();
        foreach (var stat in importStats)
        {
            if (stat.TotalQty > 0 && stat.TotalSubTotal > 0)
            {
                costMap[stat.ProductID] = stat.TotalSubTotal / stat.TotalQty;
            }
            else if (stat.AvgUnitPrice > 0)
            {
                costMap[stat.ProductID] = stat.AvgUnitPrice;
            }
        }

        var supplierPrices = await db.SupplierProducts
            .AsNoTracking()
            .Where(sp => sp.PurchasePrice > 0)
            .GroupBy(sp => sp.ProductID)
            .Select(g => new
            {
                ProductID = g.Key,
                AvgPurchasePrice = g.Average(x => x.PurchasePrice)
            })
            .ToListAsync(ct);

        foreach (var sp in supplierPrices)
        {
            if (!costMap.ContainsKey(sp.ProductID))
            {
                costMap[sp.ProductID] = sp.AvgPurchasePrice;
            }
        }

        return costMap;
    }

    private static ReportInventorySummary BuildInventorySummary(
        List<Product> activeProducts,
        Dictionary<int, decimal> unitCostMap)
    {
        var items = activeProducts
            .Select(p =>
            {
                var hasCost = unitCostMap.TryGetValue(p.ProductID, out var unitCost);
                return new ReportInventoryItem
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    CategoryName = p.Category?.CategoryName ?? "Khác",
                    StockQuantity = p.Quantity,
                    RetailPrice = p.Price,
                    UnitCost = hasCost ? unitCost : 0m,
                    HasUnitCost = hasCost
                };
            })
            .OrderBy(i => i.StockQuantity)
            .ThenBy(i => i.ProductName)
            .ToList();

        var lowStock = items.Where(i => i.StockQuantity <= 10).ToList();

        return new ReportInventorySummary
        {
            ActiveProductsCount = items.Count,
            TotalStockUnits = items.Sum(i => i.StockQuantity),
            TotalInventoryCostValue = items.Sum(i => i.InventoryCostValue),
            TotalInventoryRetailValue = items.Sum(i => i.InventoryRetailValue),
            OutOfStockCount = items.Count(i => i.StockQuantity <= 0),
            LowStockCount = lowStock.Count,
            AllItems = items,
            LowStockItems = lowStock
        };
    }

    private static ReportOrderItem MapOrderItem(
        Order order,
        Dictionary<int, decimal> unitCostMap,
        List<TimelineSlot> slots)
    {
        var isOnline = IsOnlineOrder(order.ShippingAddress);
        var customerName = order.Customer != null && !string.IsNullOrWhiteSpace(order.Customer.FullName)
            ? order.Customer.FullName
            : !string.IsNullOrWhiteSpace(order.ReceiverName) ? order.ReceiverName : "Khách lẻ";
        var customerPhone = !string.IsNullOrWhiteSpace(order.Customer?.Phone)
            ? order.Customer!.Phone
            : order.ReceiverPhone;

        var ratio = order.TotalAmount > 0 ? order.FinalAmount / order.TotalAmount : 1m;
        var details = order.OrderDetails.ToList();
        var lines = new List<ReportOrderLineItem>(details.Count);
        decimal allocatedRunningTotal = 0m;

        for (var idx = 0; idx < details.Count; idx++)
        {
            var detail = details[idx];
            var unitCost = unitCostMap.TryGetValue(detail.ProductID, out var cost) ? cost : 0m;

            // Allocate invoice discount proportionally so sum(AllocatedNetRevenue) == order.FinalAmount
            decimal allocatedNet;
            if (idx == details.Count - 1 && details.Count > 0 && order.TotalAmount > 0)
            {
                allocatedNet = order.FinalAmount - allocatedRunningTotal;
            }
            else
            {
                allocatedNet = Math.Round(detail.SubTotal * ratio, 2, MidpointRounding.AwayFromZero);
                allocatedRunningTotal += allocatedNet;
            }

            lines.Add(new ReportOrderLineItem
            {
                ProductID = detail.ProductID,
                CategoryID = detail.Product?.CategoryID ?? 0,
                ProductName = detail.Product?.ProductName ?? $"Sản phẩm #{detail.ProductID}",
                CategoryName = detail.Product?.Category?.CategoryName ?? "Khác",
                Quantity = detail.Quantity,
                UnitPrice = detail.UnitPrice,
                SubTotal = detail.SubTotal,
                AllocatedNetRevenue = allocatedNet,
                LineCost = detail.Quantity * unitCost
            });
        }

        var cogs = lines.Sum(l => l.LineCost);
        var slotKey = slots.FirstOrDefault(s => order.OrderDate >= s.Start && order.OrderDate <= s.End)?.Key ?? string.Empty;

        var statusGroup = order.Status switch
        {
            OrderStatus.Completed => "completed",
            OrderStatus.Cancelled => "cancelled",
            _ => "processing"
        };

        return new ReportOrderItem
        {
            OrderID = order.OrderID,
            CustomerID = order.CustomerID,
            OrderDate = order.OrderDate,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            EmployeeName = order.Employee?.FullName,
            ChannelKey = isOnline ? "online" : "pos",
            ChannelLabel = isOnline ? "Đặt Online" : "Tại quầy POS",
            ShippingAddress = order.ShippingAddress,
            PaymentGroupKey = ResolvePaymentGroupKey(order.PaymentMethod),
            PaymentMethodLabel = GetPaymentMethodLabel(order.PaymentMethod),
            Status = order.Status,
            StatusLabel = GetOrderStatusLabel(order.Status),
            StatusClass = GetOrderStatusClass(order.Status),
            StatusGroup = statusGroup,
            TotalAmount = order.TotalAmount,
            DiscountAmount = order.DiscountAmount,
            FinalAmount = order.FinalAmount,
            CostOfGoodsSold = cogs,
            TimelineBucketKey = slotKey,
            Lines = lines
        };
    }

    private static ReportImportItem MapImportItem(ImportReceipt receipt, List<TimelineSlot> slots)
    {
        var slotKey = slots.FirstOrDefault(s => receipt.ReceiptDate >= s.Start && receipt.ReceiptDate <= s.End)?.Key ?? string.Empty;
        return new ReportImportItem
        {
            ReceiptID = receipt.ReceiptID,
            ReceiptDate = receipt.ReceiptDate,
            SupplierName = receipt.Supplier?.SupplierName ?? $"NCC #{receipt.SupplierID}",
            EmployeeName = receipt.Employee?.FullName ?? $"NV #{receipt.EmployeeID}",
            TotalAmount = receipt.TotalAmount,
            Note = receipt.Note,
            TimelineBucketKey = slotKey,
            Lines = receipt.ImportReceiptDetails.Select(d => new ReportImportLineItem
            {
                ProductID = d.ProductID,
                ProductName = d.Product?.ProductName ?? $"Sản phẩm #{d.ProductID}",
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                SubTotal = d.SubTotal
            }).ToList()
        };
    }

    private static List<TimelineSlot> CreateTimelineSlots(
        ReportAnalyticsRequest request,
        List<Order> orders,
        List<ImportReceipt> imports)
    {
        var slots = new List<TimelineSlot>();

        if (request.TimelineMode == "hours")
        {
            var baseDate = (request.StartDate ?? DateTime.Today).Date;
            var intervals = new[]
            {
                (Key: "h06", Label: "06:00 - 09:00", Short: "06-09h", StartH: 6, EndH: 9),
                (Key: "h09", Label: "09:00 - 12:00", Short: "09-12h", StartH: 9, EndH: 12),
                (Key: "h12", Label: "12:00 - 15:00", Short: "12-15h", StartH: 12, EndH: 15),
                (Key: "h15", Label: "15:00 - 18:00", Short: "15-18h", StartH: 15, EndH: 18),
                (Key: "h18", Label: "18:00 - 21:00", Short: "18-21h", StartH: 18, EndH: 21),
                (Key: "h21", Label: "21:00 - 23:59", Short: "21-24h", StartH: 21, EndH: 24)
            };

            foreach (var item in intervals)
            {
                var start = baseDate.AddHours(item.StartH);
                var end = item.EndH == 24
                    ? baseDate.AddDays(1).AddTicks(-1)
                    : baseDate.AddHours(item.EndH).AddTicks(-1);
                slots.Add(new TimelineSlot(item.Key, item.Label, item.Short, start, end));
            }
            return slots;
        }

        if (request.TimelineMode == "weeks")
        {
            var year = request.StartDate?.Year ?? request.ReferenceYear;
            var month = request.StartDate?.Month ?? request.ReferenceMonth;
            var daysInMonth = DateTime.DaysInMonth(year, month);

            for (int w = 1; w <= 4; w++)
            {
                int dayStart = (w - 1) * 7 + 1;
                int dayEnd = w == 4 ? daysInMonth : Math.Min(w * 7, daysInMonth);
                if (dayStart > daysInMonth) break;

                var start = new DateTime(year, month, dayStart, 0, 0, 0);
                var end = new DateTime(year, month, dayEnd, 23, 59, 59, 999);
                slots.Add(new TimelineSlot(
                    $"w{w}",
                    $"Tuần {w} (Ngày {dayStart} - {dayEnd})",
                    $"Tuần {w}",
                    start,
                    end));
            }
            return slots;
        }

        if (request.TimelineMode == "quarter")
        {
            int startM = (request.ReferenceQuarter - 1) * 3 + 1;
            for (int m = startM; m < startM + 3; m++)
            {
                var start = new DateTime(request.ReferenceYear, m, 1);
                var end = start.AddMonths(1).AddTicks(-1);
                slots.Add(new TimelineSlot(
                    $"m{request.ReferenceYear}_{m:D2}",
                    $"Tháng {m:D2}/{request.ReferenceYear}",
                    $"T{m}",
                    start,
                    end));
            }
            return slots;
        }

        if (request.TimelineMode == "year")
        {
            for (int m = 1; m <= 12; m++)
            {
                var start = new DateTime(request.ReferenceYear, m, 1);
                var end = start.AddMonths(1).AddTicks(-1);
                slots.Add(new TimelineSlot(
                    $"m{request.ReferenceYear}_{m:D2}",
                    $"Tháng {m:D2}/{request.ReferenceYear}",
                    $"T{m}",
                    start,
                    end));
            }
            return slots;
        }

        // Default: custom / 7 days / all time
        DateTime rangeStart;
        if (request.StartDate.HasValue)
        {
            rangeStart = request.StartDate.Value.Date;
        }
        else if (orders.Count > 0 || imports.Count > 0)
        {
            var minOrder = orders.Count > 0 ? orders.Min(o => o.OrderDate).Date : DateTime.MaxValue;
            var minImport = imports.Count > 0 ? imports.Min(i => i.ReceiptDate).Date : DateTime.MaxValue;
            rangeStart = minOrder < minImport ? minOrder : minImport;
        }
        else
        {
            rangeStart = DateTime.Today.AddDays(-6);
        }

        DateTime rangeEnd;
        if (request.EndDate.HasValue)
        {
            rangeEnd = request.EndDate.Value.Date;
        }
        else if (orders.Count > 0 || imports.Count > 0)
        {
            var maxOrder = orders.Count > 0 ? orders.Max(o => o.OrderDate).Date : DateTime.MinValue;
            var maxImport = imports.Count > 0 ? imports.Max(i => i.ReceiptDate).Date : DateTime.MinValue;
            var maxData = maxOrder > maxImport ? maxOrder : maxImport;
            rangeEnd = maxData > DateTime.Today ? maxData : DateTime.Today;
        }
        else
        {
            rangeEnd = DateTime.Today;
        }

        if (rangeEnd < rangeStart)
        {
            rangeEnd = rangeStart;
        }

        var daysCount = (rangeEnd - rangeStart).Days + 1;
        if (daysCount <= 14)
        {
            for (var cur = rangeStart; cur <= rangeEnd; cur = cur.AddDays(1))
            {
                var start = cur.Date;
                var end = start.AddDays(1).AddTicks(-1);
                slots.Add(new TimelineSlot(
                    $"d{start:yyyyMMdd}",
                    start.ToString("dd/MM/yyyy"),
                    start.ToString("dd/MM"),
                    start,
                    end));
            }
        }
        else
        {
            var cur = new DateTime(rangeStart.Year, rangeStart.Month, 1);
            var endMonth = new DateTime(rangeEnd.Year, rangeEnd.Month, 1);
            while (cur <= endMonth)
            {
                var start = cur;
                var end = cur.AddMonths(1).AddTicks(-1);
                slots.Add(new TimelineSlot(
                    $"m{cur:yyyyMM}",
                    $"Tháng {cur.Month:D2}/{cur.Year}",
                    $"T{cur.Month}/{cur.Year % 100:D2}",
                    start,
                    end));
                cur = cur.AddMonths(1);
            }
        }

        return slots;
    }

    private static List<ReportTimelineItem> BuildTimelineItems(
        List<TimelineSlot> slots,
        List<ReportOrderItem> orderItems,
        List<ReportImportItem> importItems)
    {
        return slots.Select(slot =>
        {
            var slotCompletedOrders = orderItems
                .Where(o => o.TimelineBucketKey == slot.Key && o.Status == OrderStatus.Completed)
                .ToList();
            var slotCancelledCount = orderItems
                .Count(o => o.TimelineBucketKey == slot.Key && o.Status == OrderStatus.Cancelled);
            var slotImports = importItems
                .Where(i => i.TimelineBucketKey == slot.Key)
                .ToList();

            return new ReportTimelineItem
            {
                BucketKey = slot.Key,
                TimeLabel = slot.Label,
                ShortLabel = slot.ShortLabel,
                CompletedOrderCount = slotCompletedOrders.Count,
                CancelledOrderCount = slotCancelledCount,
                Revenue = slotCompletedOrders.Sum(o => o.FinalAmount),
                CostOfGoodsSold = slotCompletedOrders.Sum(o => o.CostOfGoodsSold),
                ImportCashflow = slotImports.Sum(i => i.TotalAmount),
                ImportReceiptsCount = slotImports.Count
            };
        }).ToList();
    }

    private static List<ReportChannelItem> BuildChannelBreakdown(List<ReportOrderItem> completedOrders, decimal totalRevenue)
    {
        var specs = new[]
        {
            (Key: "pos", Label: "Bán tại quầy (POS)", Icon: "solar:shop-2-bold"),
            (Key: "online", Label: "Đặt Online (Web / Giao hàng)", Icon: "solar:bag-smile-bold")
        };

        return specs.Select(spec =>
        {
            var matched = completedOrders.Where(o => o.ChannelKey == spec.Key).ToList();
            var rev = matched.Sum(o => o.FinalAmount);
            return new ReportChannelItem
            {
                ChannelKey = spec.Key,
                DisplayName = spec.Label,
                OrderCount = matched.Count,
                Revenue = rev,
                CostOfGoodsSold = matched.Sum(o => o.CostOfGoodsSold),
                Percentage = totalRevenue > 0 ? rev / totalRevenue * 100m : 0m,
                Icon = spec.Icon
            };
        }).ToList();
    }

    private static List<ReportPaymentItem> BuildPaymentBreakdown(List<ReportOrderItem> completedOrders, decimal totalRevenue)
    {
        var specs = new[]
        {
            (Key: "cod_cash", Label: "Tiền mặt / COD", Icon: "solar:wallet-money-bold", Tone: "emerald"),
            (Key: "bank_transfer", Label: "Chuyển khoản / VietQR", Icon: "solar:card-transfer-bold", Tone: "blue"),
            (Key: "vnpay", Label: "VNPAY", Icon: "solar:qr-code-bold", Tone: "violet"),
            (Key: "momo", Label: "Ví MoMo", Icon: "solar:smartphone-2-bold", Tone: "pink"),
            (Key: "other", Label: "Phương thức khác", Icon: "solar:card-bold", Tone: "slate")
        };

        var result = new List<ReportPaymentItem>();
        foreach (var spec in specs)
        {
            var matched = completedOrders.Where(o => o.PaymentGroupKey == spec.Key).ToList();
            if (matched.Count == 0 && spec.Key is "vnpay" or "momo" or "other")
            {
                continue;
            }

            var amount = matched.Sum(o => o.FinalAmount);
            result.Add(new ReportPaymentItem
            {
                GroupKey = spec.Key,
                DisplayName = spec.Label,
                OrderCount = matched.Count,
                Amount = amount,
                Percentage = totalRevenue > 0 ? amount / totalRevenue * 100m : 0m,
                Icon = spec.Icon,
                BadgeTone = spec.Tone
            });
        }

        return result;
    }

    private static List<ReportCategoryItem> BuildCategoryBreakdown(List<ReportOrderItem> completedOrders, decimal totalRevenue)
    {
        var grouped = completedOrders
            .SelectMany(o => o.Lines)
            .GroupBy(l => new { l.CategoryID, l.CategoryName })
            .Select(g => new
            {
                g.Key.CategoryID,
                g.Key.CategoryName,
                ProductCount = g.Select(x => x.ProductID).Distinct().Count(),
                SoldQuantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.AllocatedNetRevenue),
                Cogs = g.Sum(x => x.LineCost)
            })
            .OrderByDescending(c => c.Revenue)
            .ToList();

        return grouped.Select((c, idx) => new ReportCategoryItem
        {
            CategoryID = c.CategoryID,
            CategoryName = c.CategoryName,
            ProductCount = c.ProductCount,
            SoldQuantity = c.SoldQuantity,
            Revenue = c.Revenue,
            CostOfGoodsSold = c.Cogs,
            Percentage = totalRevenue > 0 ? c.Revenue / totalRevenue * 100m : 0m,
            ColorHex = CategoryPalette[idx % CategoryPalette.Length]
        }).ToList();
    }

    private static List<ReportProductItem> BuildProductPerformance(
        List<Product> activeProducts,
        List<ReportOrderItem> completedOrders,
        Dictionary<int, decimal> unitCostMap)
    {
        var salesByProduct = completedOrders
            .SelectMany(o => o.Lines.Select(l => new { o.OrderID, Line = l }))
            .GroupBy(x => x.Line.ProductID)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    ProductName = g.First().Line.ProductName,
                    CategoryID = g.First().Line.CategoryID,
                    CategoryName = g.First().Line.CategoryName,
                    RetailPrice = g.First().Line.UnitPrice,
                    SoldQuantity = g.Sum(x => x.Line.Quantity),
                    OrderCount = g.Select(x => x.OrderID).Distinct().Count(),
                    GrossSales = g.Sum(x => x.Line.SubTotal),
                    NetRevenue = g.Sum(x => x.Line.AllocatedNetRevenue),
                    TotalCost = g.Sum(x => x.Line.LineCost)
                });

        var result = new List<ReportProductItem>();
        var seenIds = new HashSet<int>();

        foreach (var product in activeProducts)
        {
            seenIds.Add(product.ProductID);
            var hasCost = unitCostMap.TryGetValue(product.ProductID, out var unitCost);
            salesByProduct.TryGetValue(product.ProductID, out var sale);

            result.Add(new ReportProductItem
            {
                ProductID = product.ProductID,
                CategoryID = product.CategoryID,
                ProductName = product.ProductName,
                CategoryName = product.Category?.CategoryName ?? "Khác",
                RetailPrice = product.Price,
                UnitCost = hasCost ? unitCost : 0m,
                HasUnitCost = hasCost,
                CurrentStock = product.Quantity,
                SoldQuantity = sale?.SoldQuantity ?? 0,
                OrderCount = sale?.OrderCount ?? 0,
                GrossSalesBeforeVoucher = sale?.GrossSales ?? 0m,
                NetRevenue = sale?.NetRevenue ?? 0m,
                TotalCost = sale?.TotalCost ?? 0m
            });
        }

        // Also include inactive products that were sold during the selected period so totals reconcile 100%
        foreach (var kv in salesByProduct)
        {
            if (seenIds.Contains(kv.Key)) continue;
            var hasCost = unitCostMap.TryGetValue(kv.Key, out var unitCost);
            var sale = kv.Value;

            result.Add(new ReportProductItem
            {
                ProductID = kv.Key,
                CategoryID = sale.CategoryID,
                ProductName = sale.ProductName,
                CategoryName = sale.CategoryName,
                RetailPrice = sale.RetailPrice,
                UnitCost = hasCost ? unitCost : 0m,
                HasUnitCost = hasCost,
                CurrentStock = 0,
                SoldQuantity = sale.SoldQuantity,
                OrderCount = sale.OrderCount,
                GrossSalesBeforeVoucher = sale.GrossSales,
                NetRevenue = sale.NetRevenue,
                TotalCost = sale.TotalCost
            });
        }

        return result
            .OrderByDescending(p => p.SoldQuantity)
            .ThenByDescending(p => p.NetRevenue)
            .ToList();
    }

    private static List<ReportCustomerItem> BuildTopCustomers(IEnumerable<Order> completedOrders)
    {
        return completedOrders
            .GroupBy(o => o.CustomerID)
            .Select(g =>
            {
                var first = g.OrderByDescending(x => x.OrderDate).First();
                var name = first.Customer != null && !string.IsNullOrWhiteSpace(first.Customer.FullName)
                    ? first.Customer.FullName
                    : !string.IsNullOrWhiteSpace(first.ReceiverName) ? first.ReceiverName : $"Khách hàng #{g.Key}";
                var phone = !string.IsNullOrWhiteSpace(first.Customer?.Phone)
                    ? first.Customer!.Phone
                    : first.ReceiverPhone;

                return new ReportCustomerItem
                {
                    CustomerID = g.Key,
                    CustomerName = name,
                    Phone = phone,
                    CompletedOrderCount = g.Count(),
                    TotalSpent = g.Sum(x => x.FinalAmount),
                    LastOrderDate = g.Max(x => x.OrderDate)
                };
            })
            .OrderByDescending(c => c.TotalSpent)
            .ThenByDescending(c => c.CompletedOrderCount)
            .Take(8)
            .ToList();
    }

    private static decimal? ComputeGrowthPercent(decimal current, decimal previous)
    {
        if (previous == 0m)
        {
            return current == 0m ? 0m : null;
        }
        return ((current - previous) / Math.Abs(previous)) * 100m;
    }

    private static bool IsOnlineOrder(string? shippingAddress) =>
        !string.IsNullOrWhiteSpace(shippingAddress) &&
        !shippingAddress.Contains("Nhận tại quầy", StringComparison.OrdinalIgnoreCase) &&
        !shippingAddress.Contains("Mua trực tiếp tại", StringComparison.OrdinalIgnoreCase);

    private static string ResolvePaymentGroupKey(string? method)
    {
        if (string.Equals(method, "Cash", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(method, PaymentMethod.COD, StringComparison.OrdinalIgnoreCase))
        {
            return "cod_cash";
        }
        if (string.Equals(method, PaymentMethod.BankTransfer, StringComparison.OrdinalIgnoreCase))
        {
            return "bank_transfer";
        }
        if (string.Equals(method, PaymentMethod.VNPAY, StringComparison.OrdinalIgnoreCase))
        {
            return "vnpay";
        }
        if (string.Equals(method, PaymentMethod.MoMo, StringComparison.OrdinalIgnoreCase))
        {
            return "momo";
        }
        return "other";
    }

    private static string GetPaymentMethodLabel(string? method) => method switch
    {
        PaymentMethod.COD or "Cash" => "Tiền mặt / COD",
        PaymentMethod.BankTransfer => "Chuyển khoản / QR",
        PaymentMethod.VNPAY => "VNPAY",
        PaymentMethod.MoMo => "MoMo",
        _ => string.IsNullOrWhiteSpace(method) ? "Khác" : method
    };

    private static string GetOrderStatusLabel(string? status) => status switch
    {
        OrderStatus.Pending => "Chờ duyệt",
        OrderStatus.Confirmed => "Đã duyệt",
        OrderStatus.InProgress => "Đang chuẩn bị",
        OrderStatus.Shipping => "Đang giao",
        OrderStatus.Delivered => "Đã giao",
        OrderStatus.Completed => "Hoàn tất",
        OrderStatus.Cancelled => "Đã hủy",
        _ => status ?? "Chưa xác định"
    };

    private static string GetOrderStatusClass(string? status) => status switch
    {
        OrderStatus.Completed => "is-success",
        OrderStatus.Cancelled => "is-danger",
        OrderStatus.Confirmed or OrderStatus.InProgress or OrderStatus.Shipping or OrderStatus.Delivered => "is-info",
        _ => "is-warning"
    };

    private sealed record TimelineSlot(
        string Key,
        string Label,
        string ShortLabel,
        DateTime Start,
        DateTime End);
}
