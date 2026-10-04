using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.Application.Features.Auth;

public class AuthService : IAuthService
{
    private readonly IBakeryDbContext _context;

    public AuthService(IBakeryDbContext context)
    {
        _context = context;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new LoginResult { Success = false, ErrorMessage = "Vui lòng nhập tên đăng nhập và mật khẩu." };
        }

        var normalizedUsername = request.Username.Trim();
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Username == normalizedUsername);

        if (account == null)
        {
            return new LoginResult { Success = false, ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác." };
        }

        // Kiểm tra mật khẩu (hỗ trợ cả BCrypt hash và fallback chuỗi để an toàn tuyệt đối)
        bool isPasswordValid = false;
        try
        {
            if (account.Password.StartsWith("$2a$") || account.Password.StartsWith("$2b$") || account.Password.StartsWith("$2y$"))
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, account.Password);
            }
            else
            {
                isPasswordValid = account.Password == request.Password;
            }
        }
        catch
        {
            isPasswordValid = account.Password == request.Password;
        }

        if (!isPasswordValid)
        {
            return new LoginResult { Success = false, ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác." };
        }

        // Kiểm tra trạng thái tài khoản
        if (account.Status == AccountStatus.Inactive)
        {
            return new LoginResult { Success = false, ErrorMessage = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên." };
        }

        // Chỉ Admin và Employee mới được phép đăng nhập WebAdmin
        if (account.Role == AccountRole.Customer)
        {
            return new LoginResult { Success = false, ErrorMessage = "Tài khoản khách hàng không có quyền truy cập hệ thống quản trị." };
        }

        // Lấy tên hiển thị từ Employee nếu có
        string? fullName = null;
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.AccountID == account.AccountID);
        if (employee != null && !string.IsNullOrWhiteSpace(employee.FullName))
        {
            fullName = employee.FullName;
        }

        return new LoginResult
        {
            Success = true,
            AccountId = account.AccountID,
            Username = account.Username,
            Role = account.Role,
            FullName = fullName ?? account.Username
        };
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return false;

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return false;

        var account = await _context.Accounts.FindAsync(request.AccountId);
        if (account == null)
            return false;

        bool isOldPasswordValid = false;
        try
        {
            if (account.Password.StartsWith("$2a$") || account.Password.StartsWith("$2b$") || account.Password.StartsWith("$2y$"))
            {
                isOldPasswordValid = BCrypt.Net.BCrypt.Verify(request.OldPassword, account.Password);
            }
            else
            {
                isOldPasswordValid = account.Password == request.OldPassword;
            }
        }
        catch
        {
            isOldPasswordValid = account.Password == request.OldPassword;
        }

        if (!isOldPasswordValid)
            return false;

        account.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();
        return true;
    }
}

