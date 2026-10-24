using BakerySystem.Domain.Entities;
using BakerySystem.Domain.Enums;
using BakerySystem.MobileApp.Api;

namespace BakerySystem.MobileApp.Services;

public class CartItem
{
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ImageURL { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public int MaxStock { get; set; } = 999;

    public decimal SubTotal => UnitPrice * Quantity;
}

public class CartService
{
    private readonly MobileApiClient _apiClient;

    public event Action? OnChange;

    public List<CartItem> Items { get; private set; } = new();
    public string AppliedVoucherCode { get; private set; } = string.Empty;
    private int? AppliedPromotionId { get; set; }
    private Promotion? appliedPromotion;
    public decimal DiscountPercent { get; private set; } = 0;
    public decimal MaxDiscountAmount { get; private set; } = 0;
    public decimal MinOrderAmount { get; private set; } = 0;

    public CartService(MobileApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public int TotalItemsCount => Items.Sum(i => i.Quantity);

    public decimal SubTotalAmount => Items.Sum(i => i.SubTotal);

    public bool IsVoucherEligible => !string.IsNullOrEmpty(AppliedVoucherCode) && (MinOrderAmount <= 0 || SubTotalAmount >= MinOrderAmount);
    public decimal RemainingForVoucher => Math.Max(0, MinOrderAmount - SubTotalAmount);

    public decimal InvoiceDiscountAmount
    {
        get
        {
            var invoicePromotion = appliedPromotion?.PromotionInvoice;
            if (invoicePromotion is null || SubTotalAmount < invoicePromotion.MinOrderAmount)
                return 0;

            var discount = SubTotalAmount * (invoicePromotion.DiscountPercent / 100m);
            if (invoicePromotion.MaxDiscountAmount is > 0 && discount > invoicePromotion.MaxDiscountAmount.Value)
                discount = invoicePromotion.MaxDiscountAmount.Value;
            return Math.Min(SubTotalAmount, discount);
        }
    }

    public decimal ProductDiscountAmount
    {
        get
        {
            if (appliedPromotion is null || appliedPromotion.PromotionProducts.Count == 0)
                return 0;

            decimal discountTotal = 0;
            foreach (var item in Items)
            {
                var condition = appliedPromotion.PromotionProducts
                    .FirstOrDefault(product => product.ProductID == item.ProductID);
                if (condition is null)
                    continue;

                var discount = item.SubTotal * (condition.DiscountPercent / 100m);
                if (condition.MaxDiscountAmount is > 0)
                    discount = Math.Min(discount, condition.MaxDiscountAmount.Value);
                discountTotal += Math.Min(item.SubTotal, discount);
            }

            return Math.Min(Math.Max(0, SubTotalAmount - InvoiceDiscountAmount), discountTotal);
        }
    }

    public decimal DiscountAmount => Math.Min(SubTotalAmount, InvoiceDiscountAmount + ProductDiscountAmount);

    public decimal FinalAmount => Math.Max(0, SubTotalAmount - DiscountAmount);

    public decimal GetProductDiscountPercent(int productId) => appliedPromotion?.PromotionProducts
        .FirstOrDefault(item => item.ProductID == productId)?.DiscountPercent ?? 0;

    public decimal GetProductDiscountAmount(int productId, decimal lineSubtotal)
    {
        var condition = appliedPromotion?.PromotionProducts
            .FirstOrDefault(item => item.ProductID == productId);
        if (condition is null || lineSubtotal <= 0)
            return 0;

        var discount = lineSubtotal * condition.DiscountPercent / 100m;
        if (condition.MaxDiscountAmount is > 0)
            discount = Math.Min(discount, condition.MaxDiscountAmount.Value);
        return Math.Min(lineSubtotal, discount);
    }

    public string AppliedPromotionSummary
    {
        get
        {
            if (appliedPromotion is null)
                return string.Empty;

            var summaries = new List<string>();
            if (appliedPromotion.PromotionInvoice is { } invoice)
            {
                var cap = invoice.MaxDiscountAmount is > 0
                    ? $" (tối đa {invoice.MaxDiscountAmount.Value:N0}đ)"
                    : string.Empty;
                summaries.Add($"giảm {invoice.DiscountPercent:0.##}% đơn từ {invoice.MinOrderAmount:N0}đ{cap}");
            }

            if (appliedPromotion.PromotionProducts.Count > 0)
            {
                var names = string.Join(", ", appliedPromotion.PromotionProducts
                    .Select(item => item.Product?.ProductName)
                    .Where(name => !string.IsNullOrWhiteSpace(name)));
                var percent = appliedPromotion.PromotionProducts.First().DiscountPercent;
                var productCap = appliedPromotion.PromotionProducts.First().MaxDiscountAmount;
                var cap = productCap is > 0
                    ? $" (tối đa {productCap.Value:N0}đ/bánh)"
                    : string.Empty;
                summaries.Add($"giảm {percent:0.##}% cho {names}{cap}");
            }

            return string.Join("; ", summaries);
        }
    }

    public void AddItem(Product product, int quantity = 1)
    {
        if (product.Quantity <= 0) return;

        var existing = Items.FirstOrDefault(i => i.ProductID == product.ProductID);
        if (existing != null)
        {
            existing.MaxStock = product.Quantity;
            existing.Quantity = Math.Min(existing.Quantity + quantity, product.Quantity);
        }
        else
        {
            var rawImg = !string.IsNullOrWhiteSpace(product.ImageURL) ? product.ImageURL.Trim() : "/assets/images/products/default-bakery.svg";
            string resolvedImg = rawImg;
            if (!rawImg.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !rawImg.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                resolvedImg = rawImg.Replace('\\', '/').TrimStart('/');
                if (resolvedImg.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase))
                {
                    resolvedImg = resolvedImg["wwwroot/".Length..];
                }

                resolvedImg = resolvedImg.StartsWith("assets/", StringComparison.OrdinalIgnoreCase)
                    ? $"/{resolvedImg}"
                    : "/assets/images/products/default-bakery.svg";
            }

            Items.Add(new CartItem
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName,
                ImageURL = resolvedImg,
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
        if (item != null)
        {
            if (delta > 0 && item.Quantity >= item.MaxStock)
            {
                return; // Đã đạt số lượng tồn kho tối đa
            }

            item.Quantity += delta;
            if (item.Quantity <= 0)
            {
                Items.Remove(item);
            }
            NotifyStateChanged();
        }
    }

    public void RemoveItem(int productId)
    {
        Items.RemoveAll(i => i.ProductID == productId);
        NotifyStateChanged();
    }

    public void Clear()
    {
        Items.Clear();
        AppliedVoucherCode = string.Empty;
        AppliedPromotionId = null;
        appliedPromotion = null;
        DiscountPercent = 0;
        MaxDiscountAmount = 0;
        MinOrderAmount = 0;
        NotifyStateChanged();
    }

    public void RemoveVoucher()
    {
        AppliedVoucherCode = string.Empty;
        AppliedPromotionId = null;
        appliedPromotion = null;
        DiscountPercent = 0;
        MaxDiscountAmount = 0;
        MinOrderAmount = 0;
        NotifyStateChanged();
    }

    public async Task<bool> ApplyVoucherAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        var promo = await _apiClient.FindPromotionAsync(code);
        if (promo is null || (promo.PromotionInvoice is null && promo.PromotionProducts.Count == 0))
            return false;

        AppliedVoucherCode = promo.PromotionCode;
        AppliedPromotionId = promo.PromotionID;
        appliedPromotion = promo;
        DiscountPercent = promo.PromotionInvoice?.DiscountPercent
            ?? promo.PromotionProducts.FirstOrDefault()?.DiscountPercent
            ?? 0;
        MaxDiscountAmount = promo.PromotionInvoice?.MaxDiscountAmount
            ?? promo.PromotionProducts.FirstOrDefault()?.MaxDiscountAmount
            ?? 0;
        MinOrderAmount = promo.PromotionInvoice?.MinOrderAmount ?? 0;
        NotifyStateChanged();
        return true;
    }

    public Task<Order?> PlaceOrderAsync(string receiverName, string phone, string address, string note, string paymentMethod)
    {
        return PlaceOrderAsync(null, receiverName, phone, address, note, paymentMethod);
    }

    public async Task<Order?> PlaceOrderAsync(int? customerId, string receiverName, string phone, string address, string note, string paymentMethod)
    {
        if (!Items.Any()) return null;

        var request = new CreateMobileOrderRequest(
            customerId,
            receiverName,
            phone,
            address,
            note,
            paymentMethod,
            SubTotalAmount,
            DiscountAmount,
            FinalAmount,
            AppliedPromotionId,
            Items.Select(item => new CreateMobileOrderLine(
                item.ProductID,
                item.Quantity,
                item.UnitPrice,
                item.SubTotal)).ToList());

        var order = await _apiClient.CreateOrderAsync(request);
        if (order != null)
            Clear();
        return order;
    }

    public Task<bool> CancelOrderAsync(int orderId, int? customerId = null) =>
        _apiClient.CancelOrderAsync(orderId, customerId);

    public void ReOrder(Order order)
    {
        if (order.OrderDetails == null || !order.OrderDetails.Any()) return;

        foreach (var detail in order.OrderDetails)
        {
            if (detail.Product != null && detail.Product.IsActive && detail.Product.Quantity > 0)
            {
                AddItem(detail.Product, Math.Min(detail.Quantity, detail.Product.Quantity));
            }
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

}
