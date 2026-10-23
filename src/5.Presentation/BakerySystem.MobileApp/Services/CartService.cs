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

    public decimal DiscountAmount
    {
        get
        {
            if (DiscountPercent <= 0) return 0;
            if (MinOrderAmount > 0 && SubTotalAmount < MinOrderAmount) return 0;
            var discount = SubTotalAmount * (DiscountPercent / 100m);
            if (MaxDiscountAmount > 0 && discount > MaxDiscountAmount)
                return MaxDiscountAmount;
            return discount;
        }
    }

    public decimal FinalAmount => Math.Max(0, SubTotalAmount - DiscountAmount);

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
        DiscountPercent = 0;
        MaxDiscountAmount = 0;
        MinOrderAmount = 0;
        NotifyStateChanged();
    }

    public void RemoveVoucher()
    {
        AppliedVoucherCode = string.Empty;
        AppliedPromotionId = null;
        DiscountPercent = 0;
        MaxDiscountAmount = 0;
        MinOrderAmount = 0;
        NotifyStateChanged();
    }

    public async Task<bool> ApplyVoucherAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        var promo = await _apiClient.FindPromotionAsync(code);
        if (promo?.PromotionInvoice == null)
            return false;

        AppliedVoucherCode = promo.PromotionCode;
        AppliedPromotionId = promo.PromotionID;
        DiscountPercent = promo.PromotionInvoice.DiscountPercent;
        MaxDiscountAmount = promo.PromotionInvoice.MaxDiscountAmount ?? 100000m;
        MinOrderAmount = promo.PromotionInvoice.MinOrderAmount;
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
