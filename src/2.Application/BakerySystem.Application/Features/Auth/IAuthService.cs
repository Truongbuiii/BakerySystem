namespace BakerySystem.Application.Features.Auth;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request);
    Task<bool> ChangePasswordAsync(ChangePasswordRequest request);
}
