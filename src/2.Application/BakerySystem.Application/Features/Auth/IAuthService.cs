namespace BakerySystem.Application.Features.Auth;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request);
    Task<bool> ChangePasswordAsync(ChangePasswordRequest request);
    Task<string?> GetSessionStampAsync(int accountId, CancellationToken ct = default);
    Task<bool> IsSessionValidAsync(int accountId, string? expectedRole, string? expectedStamp, CancellationToken ct = default);

    // Customer Authentication & Profile
    Task<CustomerAuthResult> CustomerLoginAsync(LoginRequest request);
    Task<CustomerAuthResult> RegisterCustomerAsync(CustomerRegisterRequest request);
    Task<bool> UpdateCustomerProfileAsync(UpdateCustomerProfileRequest request);
    Task<CustomerDto?> GetCustomerProfileAsync(int customerId);
    Task<CustomerDto?> GetCustomerByAccountIdAsync(int accountId);
}

