using System.Net.Http.Json;
using BakerySystem.Application.Features.Auth;
using BakerySystem.Domain.Entities;
using BakerySystem.MobileApp.Api;

namespace BakerySystem.MobileApp.Services;

public sealed class MobileApiClient(HttpClient httpClient)
{
    public async Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default) =>
        await httpClient.GetFromJsonAsync<List<Category>>("api/mobile/categories", ct) ?? [];

    public async Task<List<Product>> GetProductsAsync(CancellationToken ct = default) =>
        await httpClient.GetFromJsonAsync<List<Product>>("api/mobile/products", ct) ?? [];

    public async Task<List<Promotion>> GetPromotionsAsync(CancellationToken ct = default) =>
        await httpClient.GetFromJsonAsync<List<Promotion>>("api/mobile/promotions", ct) ?? [];

    public async Task<Promotion?> FindPromotionAsync(string code, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/mobile/promotions/{Uri.EscapeDataString(code.Trim())}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Promotion>(cancellationToken: ct);
    }

    public async Task<List<Order>> GetOrdersAsync(
        int? customerId,
        string? phone,
        CancellationToken ct = default)
    {
        var query = customerId.HasValue
            ? $"?customerId={customerId.Value}"
            : $"?phone={Uri.EscapeDataString(phone ?? string.Empty)}";
        return await httpClient.GetFromJsonAsync<List<Order>>(
                   $"api/mobile/orders{query}", ct) ?? [];
    }

    public async Task<CustomerAuthResult> LoginAsync(
        LoginRequest request,
        CancellationToken ct = default) =>
        await SendAsync<LoginRequest, CustomerAuthResult>(
            HttpMethod.Post, "api/mobile/auth/login", request, ct);

    public async Task<CustomerAuthResult> RegisterAsync(
        CustomerRegisterRequest request,
        CancellationToken ct = default) =>
        await SendAsync<CustomerRegisterRequest, CustomerAuthResult>(
            HttpMethod.Post, "api/mobile/auth/register", request, ct);

    public async Task<CustomerDto?> GetProfileAsync(
        int customerId,
        CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/mobile/customers/{customerId}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerDto>(cancellationToken: ct);
    }

    public async Task<bool> UpdateProfileAsync(
        UpdateCustomerProfileRequest request,
        CancellationToken ct = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            "api/mobile/customers/profile", request, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/mobile/customers/change-password", request, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<Order?> CreateOrderAsync(
        CreateMobileOrderRequest request,
        CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/mobile/orders", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Order>(cancellationToken: ct);
    }

    public async Task<bool> CancelOrderAsync(
        int orderId,
        int? customerId,
        CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/mobile/orders/{orderId}/cancel",
            new CancelMobileOrderRequest(customerId),
            ct);
        return response.IsSuccessStatusCode;
    }

    private async Task<TResponse> SendAsync<TRequest, TResponse>(
        HttpMethod method,
        string uri,
        TRequest request,
        CancellationToken ct)
    {
        using var message = new HttpRequestMessage(method, uri)
        {
            Content = JsonContent.Create(request)
        };
        using var response = await httpClient.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("The API returned an empty response.");
    }
}
