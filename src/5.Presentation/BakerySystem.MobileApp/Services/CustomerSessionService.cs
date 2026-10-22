using BakerySystem.Application.Features.Auth;
using Microsoft.Maui.Storage;

namespace BakerySystem.MobileApp.Services;

public class CustomerSessionService
{
    private readonly MobileApiClient _apiClient;

    public event Action? OnAuthChanged;

    public CustomerDto? CurrentCustomer { get; private set; }
    public bool IsLoggedIn => CurrentCustomer != null;

    public CustomerSessionService(MobileApiClient apiClient)
    {
        _apiClient = apiClient;
        CurrentCustomer = null;
        ClearSavedSession();
    }

    private void ClearSavedSession()
    {
        try
        {
            Preferences.Default.Remove("Customer_IsLoggedIn");
            Preferences.Default.Remove("Customer_Id");
            Preferences.Default.Remove("Customer_AccountId");
            Preferences.Default.Remove("Customer_Username");
            Preferences.Default.Remove("Customer_FullName");
            Preferences.Default.Remove("Customer_Phone");
            Preferences.Default.Remove("Customer_Address");
        }
        catch { }
    }

    private void LoadFromPreferences()
    {
        try
        {
            bool isLoggedIn = Preferences.Default.Get("Customer_IsLoggedIn", false);
            if (isLoggedIn)
            {
                int customerId = Preferences.Default.Get("Customer_Id", 0);
                int accountId = Preferences.Default.Get("Customer_AccountId", 0);
                string username = Preferences.Default.Get("Customer_Username", string.Empty);
                string fullName = Preferences.Default.Get("Customer_FullName", string.Empty);
                string phone = Preferences.Default.Get("Customer_Phone", string.Empty);
                string address = Preferences.Default.Get("Customer_Address", string.Empty);

                if (customerId > 0)
                {
                    CurrentCustomer = new CustomerDto
                    {
                        CustomerId = customerId,
                        AccountId = accountId > 0 ? accountId : null,
                        Username = username,
                        FullName = fullName,
                        Phone = phone,
                        Address = address
                    };
                }
            }
        }
        catch
        {
            // Fallback an toàn nếu Preferences chưa sẵn sàng
        }
    }

    public async Task RefreshProfileAsync()
    {
        if (CurrentCustomer == null) return;
        try
        {
            var fresh = await _apiClient.GetProfileAsync(CurrentCustomer.CustomerId);
            if (fresh != null)
            {
                CurrentCustomer = fresh;
                SaveToPreferences(fresh);
                OnAuthChanged?.Invoke();
            }
        }
        catch { }
    }

    public async Task<CustomerAuthResult> LoginAsync(string username, string password)
    {
        var result = await _apiClient.LoginAsync(new LoginRequest
        {
            Username = username,
            Password = password
        });

        if (result.Success && result.Customer != null)
        {
            CurrentCustomer = result.Customer;
            SaveToPreferences(result.Customer);
            OnAuthChanged?.Invoke();
        }

        return result;
    }

    public async Task<CustomerAuthResult> RegisterAsync(CustomerRegisterRequest request)
    {
        // Đăng ký chỉ tạo tài khoản; khách hàng phải đăng nhập riêng để vào ứng dụng.
        return await _apiClient.RegisterAsync(request);
    }

    public async Task<bool> UpdateProfileAsync(string fullName, string phone, string address)
    {
        if (CurrentCustomer == null) return false;

        var ok = await _apiClient.UpdateProfileAsync(new UpdateCustomerProfileRequest
        {
            CustomerId = CurrentCustomer.CustomerId,
            FullName = fullName,
            Phone = phone,
            Address = address
        });

        if (ok)
        {
            CurrentCustomer.FullName = fullName;
            CurrentCustomer.Phone = phone;
            CurrentCustomer.Address = address;
            SaveToPreferences(CurrentCustomer);
            OnAuthChanged?.Invoke();
        }

        return ok;
    }

    public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword, string confirmPassword)
    {
        if (CurrentCustomer?.AccountId == null) return false;

        return await _apiClient.ChangePasswordAsync(new ChangePasswordRequest
        {
            AccountId = CurrentCustomer.AccountId.Value,
            OldPassword = oldPassword,
            NewPassword = newPassword,
            ConfirmPassword = confirmPassword
        });
    }

    public void Logout()
    {
        CurrentCustomer = null;
        try
        {
            Preferences.Default.Remove("Customer_IsLoggedIn");
            Preferences.Default.Remove("Customer_Id");
            Preferences.Default.Remove("Customer_AccountId");
            Preferences.Default.Remove("Customer_Username");
            Preferences.Default.Remove("Customer_FullName");
            Preferences.Default.Remove("Customer_Phone");
            Preferences.Default.Remove("Customer_Address");
        }
        catch { }

        OnAuthChanged?.Invoke();
    }

    private void SaveToPreferences(CustomerDto customer)
    {
        try
        {
            Preferences.Default.Set("Customer_IsLoggedIn", true);
            Preferences.Default.Set("Customer_Id", customer.CustomerId);
            Preferences.Default.Set("Customer_AccountId", customer.AccountId ?? 0);
            Preferences.Default.Set("Customer_Username", customer.Username ?? string.Empty);
            Preferences.Default.Set("Customer_FullName", customer.FullName ?? string.Empty);
            Preferences.Default.Set("Customer_Phone", customer.Phone ?? string.Empty);
            Preferences.Default.Set("Customer_Address", customer.Address ?? string.Empty);
        }
        catch { }
    }
}

