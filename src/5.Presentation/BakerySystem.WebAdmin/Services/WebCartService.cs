using BakerySystem.Application.Features.CustomerOrders;
using BakerySystem.Domain.Entities;

namespace BakerySystem.WebAdmin.Services;

public sealed class WebCartItem
{
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ImageURL { get; set; } = "/assets/images/products/default-bakery.svg";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public int MaxStock { get; set; } = 999;

    public decimal SubTotal => UnitPrice * Quantity;
}

public sealed class WebCartService
{
    public event Action? OnChange;

    public List<WebCartItem> Items { get; } = [];
    public string AppliedVoucherCode { get; private set; } = string.Empty;
    public int? AppliedPromotionId { get; private set; }
    public Promotion? AppliedPromotion { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal? FixedDiscountAmount { get; private set; }
    public decimal MaxDiscountAmount { get; private set; }
    public decimal MinOrderAmount { get; private set; }

    public int TotalItemsCount => Items.Sum(i => i.Quantity);
    public decimal SubTotalAmount => Items.Sum(i => i.SubTotal);

    public decimal RemainingForVoucher =>
        AppliedPromotion?.PromotionInvoice is { } inv && SubTotalAmount < inv.MinOrderAmount
            ? inv.MinOrderAmount - SubTotalAmount
            : 0m;

    public decimal InvoiceDiscountAmount
    {
        get
        {
            var inv = AppliedPromotion?.PromotionInvoice;
            if (inv is null || SubTotalAmount < inv.MinOrderAmount)
                return 0m;

            decimal discount;
            if (inv.DiscountAmount is > 0)
            {
                discount = inv.DiscountAmount.Value;
            }
            else
            {
                discount = SubTotalAmount * (inv.DiscountPercent / 100m);
                if (inv.MaxDiscountAmount is > 0)
                    discount = Math.Min(discount, inv.MaxDiscountAmount.Value);
            }

            return Math.Min(SubTotalAmount, Math.Round(discount, 0));
        }
    }

    public decimal ProductDiscountAmount
    {
        get
        {
            if (AppliedPromotion is null || AppliedPromotion.PromotionProducts.Count == 0)
                return 0m;

            decimal discountTotal = 0m;
            foreach (var item in Items)
            {
                var condition = AppliedPromotion.PromotionProducts
                    .FirstOrDefault(p => p.ProductID == item.ProductID);
                if (condition is null)
                    continue;

                var discount = item.SubTotal * (condition.DiscountPercent / 100m);
                if (condition.MaxDiscountAmount is > 0)
                    discount = Math.Min(discount, condition.MaxDiscountAmount.Value);
                discountTotal += Math.Min(item.SubTotal, Math.Round(discount, 0));
            }

            return Math.Min(Math.Max(0m, SubTotalAmount - InvoiceDiscountAmount), discountTotal);
        }
    }

    public decimal DiscountAmount => Math.Min(SubTotalAmount, InvoiceDiscountAmount + ProductDiscountAmount);
    public decimal FinalAmount => Math.Max(0m, SubTotalAmount - DiscountAmount);

    public decimal GetProductDiscountPercent(int productId) =>
        AppliedPromotion?.PromotionProducts.FirstOrDefault(p => p.ProductID == productId)?.DiscountPercent ?? 0m;

    public decimal GetProductDiscountAmount(int productId, decimal lineSubtotal)
    {
        var condition = AppliedPromotion?.PromotionProducts.FirstOrDefault(p => p.ProductID == productId);
        if (condition is null || lineSubtotal <= 0)
            return 0m;

        var discount = lineSubtotal * (condition.DiscountPercent / 100m);
        if (condition.MaxDiscountAmount is > 0)
            discount = Math.Min(discount, condition.MaxDiscountAmount.Value);
        return Math.Min(lineSubtotal, Math.Round(discount, 0));
    }

    public string AppliedPromotionSummary
    {
        get
        {
            if (AppliedPromotion is null)
                return string.Empty;

            var parts = new List<string>();
            if (AppliedPromotion.PromotionInvoice is { } inv)
            {
                if (inv.DiscountAmount is > 0)
                {
                    parts.Add($"giảm {inv.DiscountAmount.Value:N0}đ đơn từ {inv.MinOrderAmount:N0}đ");
                }
                else
                {
                    var cap = inv.MaxDiscountAmount is > 0 ? $" (tối đa {inv.MaxDiscountAmount.Value:N0}đ)" : string.Empty;
                    parts.Add($"giảm {inv.DiscountPercent:0.##}% đơn từ {inv.MinOrderAmount:N0}đ{cap}");
                }
            }

            if (AppliedPromotion.PromotionProducts.Count > 0)
            {
                var names = string.Join(", ", AppliedPromotion.PromotionProducts
                    .Select(p => p.Product?.ProductName)
                    .Where(n => !string.IsNullOrWhiteSpace(n)));
                var first = AppliedPromotion.PromotionProducts.First();
                var cap = first.MaxDiscountAmount is > 0 ? $" (tối đa {first.MaxDiscountAmount.Value:N0}đ)" : string.Empty;
                parts.Add($"giảm {first.DiscountPercent:0.##}% cho {(string.IsNullOrWhiteSpace(names) ? "sản phẩm áp dụng" : names)}{cap}");
            }

            return string.Join("; ", parts);
        }
    }

    public void AddItem(Product product, int quantity = 1)
    {
        if (product.Quantity <= 0 || quantity <= 0)
            return;

        var existing = Items.FirstOrDefault(i => i.ProductID == product.ProductID);
        if (existing is not null)
        {
            existing.MaxStock = product.Quantity;
            existing.UnitPrice = product.Price;
            existing.Quantity = Math.Min(existing.Quantity + quantity, product.Quantity);
        }
        else
        {
            Items.Add(new WebCartItem
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName,
                ImageURL = ResolveProductImageUrl(product),
                UnitPrice = product.Price,
                Quantity = Math.Min(quantity, product.Quantity),
                MaxStock = product.Quantity
            });
        }

        NotifyStateChanged();
    }

    public void UpdateQuantity(int productId, int delta)
    {
        var item = Items.FirstOrDefault(i => i.ProductID == productId);
        if (item is null)
            return;

        if (delta > 0 && item.Quantity >= item.MaxStock)
            return;

        item.Quantity += delta;
        if (item.Quantity <= 0)
            Items.Remove(item);

        NotifyStateChanged();
    }

    public void RemoveItem(int productId)
    {
        Items.RemoveAll(i => i.ProductID == productId);
        NotifyStateChanged();
    }

    public void Clear()
    {
        Items.Clear();
        ClearVoucherState();
        NotifyStateChanged();
    }

    public void RemoveVoucher()
    {
        ClearVoucherState();
        NotifyStateChanged();
    }

    public bool ApplyPromotion(Promotion promo)
    {
        if (promo.PromotionInvoice is null && promo.PromotionProducts.Count == 0)
            return false;

        AppliedVoucherCode = promo.PromotionCode;
        AppliedPromotionId = promo.PromotionID;
        AppliedPromotion = promo;
        FixedDiscountAmount = promo.PromotionInvoice?.DiscountAmount;
        DiscountPercent = promo.PromotionInvoice?.DiscountPercent
            ?? promo.PromotionProducts.FirstOrDefault()?.DiscountPercent
            ?? 0m;
        MaxDiscountAmount = promo.PromotionInvoice?.MaxDiscountAmount
            ?? promo.PromotionProducts.FirstOrDefault()?.MaxDiscountAmount
            ?? 0m;
        MinOrderAmount = promo.PromotionInvoice?.MinOrderAmount ?? 0m;

        NotifyStateChanged();
        return true;
    }

    public async Task<bool> ApplyVoucherAsync(string code, CustomerOrderService orderService, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var promo = await orderService.FindPromotionByCodeAsync(code, ct);
        if (promo is null)
            return false;

        return ApplyPromotion(promo);
    }

    public int ReOrder(Order order, IReadOnlyDictionary<int, Product>? activeProductsById = null)
    {
        if (order.OrderDetails is null || order.OrderDetails.Count == 0)
            return 0;

        var addedCount = 0;
        foreach (var detail in order.OrderDetails)
        {
            Product? product = null;
            if (activeProductsById is not null && activeProductsById.TryGetValue(detail.ProductID, out var liveProduct))
            {
                product = liveProduct;
            }
            else if (detail.Product is not null && detail.Product.IsActive && detail.Product.Quantity > 0)
            {
                product = detail.Product;
            }

            if (product is not null && product.IsActive && product.Quantity > 0)
            {
                AddItem(product, Math.Min(detail.Quantity, product.Quantity));
                addedCount++;
            }
        }

        return addedCount;
    }

    public static string ResolveProductImageUrl(Product? product)
    {
        const string fallback = "/assets/images/products/default-bakery.svg";
        if (product is null)
            return fallback;

        var rawUrl = product.ImageURL?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawUrl))
            return fallback;

        if (rawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            rawUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return rawUrl;
        }

        rawUrl = rawUrl.Replace('\\', '/').TrimStart('~').TrimStart('/');
        if (rawUrl.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase))
            rawUrl = rawUrl["wwwroot/".Length..];

        return "/" + rawUrl;
    }

    private void ClearVoucherState()
    {
        AppliedVoucherCode = string.Empty;
        AppliedPromotionId = null;
        AppliedPromotion = null;
        FixedDiscountAmount = null;
        DiscountPercent = 0m;
        MaxDiscountAmount = 0m;
        MinOrderAmount = 0m;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
