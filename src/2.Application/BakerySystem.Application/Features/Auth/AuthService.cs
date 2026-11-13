using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Domain.Entities;
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

        // Hỗ trợ khách hàng đăng nhập linh hoạt bằng Số điện thoại
        if (account == null)
        {
            var customerByPhone = await _context.Customers
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.Phone == normalizedUsername && c.AccountID != null);
            if (customerByPhone != null && customerByPhone.Account != null)
            {
                account = customerByPhone.Account;
            }
        }

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

        // Cho phép Admin, Employee và Customer đăng nhập Web
        if (account.Role is not (AccountRole.Admin or AccountRole.Employee or AccountRole.Customer))
        {
            return new LoginResult { Success = false, ErrorMessage = "Tài khoản không có quyền truy cập hệ thống." };
        }

        // Lấy tên hiển thị theo vai trò
        string? fullName = null;
        if (account.Role == AccountRole.Customer)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.AccountID == account.AccountID);
            if (customer == null)
            {
                customer = new Customer
                {
                    AccountID = account.AccountID,
                    FullName = account.Username,
                    Phone = string.Empty,
                    Address = string.Empty
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrWhiteSpace(customer.FullName))
            {
                fullName = customer.FullName;
            }
        }
        else
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.AccountID == account.AccountID);
            if (employee != null && !string.IsNullOrWhiteSpace(employee.FullName))
            {
                fullName = employee.FullName;
            }
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

    public async Task<CustomerAuthResult> CustomerLoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new CustomerAuthResult { Success = false, ErrorMessage = "Vui lòng nhập tên đăng nhập và mật khẩu." };
        }

        var normalizedUsername = request.Username.Trim();
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Username == normalizedUsername);

        // Hỗ trợ đăng nhập linh hoạt bằng Số điện thoại nếu không khớp Username
        if (account == null)
        {
            var customerByPhone = await _context.Customers
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.Phone == normalizedUsername && c.AccountID != null);
            if (customerByPhone != null && customerByPhone.Account != null)
            {
                account = customerByPhone.Account;
            }
        }

        if (account == null)
        {
            return new CustomerAuthResult { Success = false, ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác." };
        }

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
            return new CustomerAuthResult { Success = false, ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác." };
        }

        if (account.Status == AccountStatus.Inactive)
        {
            return new CustomerAuthResult { Success = false, ErrorMessage = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ hỗ trợ." };
        }

        // Lấy thông tin khách hàng tương ứng
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.AccountID == account.AccountID);

        if (customer == null)
        {
            customer = new Customer
            {
                AccountID = account.AccountID,
                FullName = account.Username,
                Phone = string.Empty,
                Address = string.Empty
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        return new CustomerAuthResult
        {
            Success = true,
            Customer = new CustomerDto
            {
                CustomerId = customer.CustomerID,
                AccountId = account.AccountID,
                Username = account.Username,
                LastName = customer.LastName,
                FirstName = customer.FirstName,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Address = customer.Address
            }
        };
    }

    public async Task<CustomerAuthResult> RegisterCustomerAsync(CustomerRegisterRequest request)
    {
        var cleanFullName = request.FullName?.Trim() ?? string.Empty;
        if (cleanFullName.Length < 2 || cleanFullName.Length > 100 || cleanFullName.Count(char.IsLetter) < 2)
            return new CustomerAuthResult { Success = false, ErrorMessage = "Họ và tên phải từ 2 đến 100 ký tự và chứa ít nhất 2 chữ cái." };

        var normalizedUsername = request.Username?.Trim() ?? string.Empty;
        if (!Regex.IsMatch(normalizedUsername, @"^[a-zA-Z0-9_.-]{3,50}$"))
            return new CustomerAuthResult { Success = false, ErrorMessage = "Tên đăng nhập từ 3–50 ký tự, chỉ gồm chữ không dấu, số và ký tự (_, ., -), không chứa khoảng trắng." };

        var (isPhoneValid, cleanPhone, phoneError) = NormalizeAndValidatePhone(request.Phone, required: true);
        if (!isPhoneValid)
            return new CustomerAuthResult { Success = false, ErrorMessage = phoneError };

        var cleanAddress = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        if (cleanAddress is not null && (cleanAddress.Length < 5 || cleanAddress.Length > 255 || !cleanAddress.Any(char.IsLetter)))
            return new CustomerAuthResult { Success = false, ErrorMessage = "Địa chỉ giao bánh phải từ 5 đến 255 ký tự và chứa tên đường/khu vực hợp lệ." };

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6 || request.Password.Length > 100)
            return new CustomerAuthResult { Success = false, ErrorMessage = "Mật khẩu phải có từ 6 đến 100 ký tự." };

        if (request.Password != request.ConfirmPassword)
            return new CustomerAuthResult { Success = false, ErrorMessage = "Xác nhận mật khẩu không trùng khớp." };

        var existingAccount = await _context.Accounts
            .AnyAsync(a => a.Username == normalizedUsername);

        if (existingAccount)
        {
            return new CustomerAuthResult { Success = false, ErrorMessage = "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác." };
        }

        if (!string.IsNullOrWhiteSpace(cleanPhone))
        {
            var existingPhone = await _context.Customers
                .AnyAsync(c => c.Phone == cleanPhone && c.AccountID != null);
            if (existingPhone)
            {
                return new CustomerAuthResult { Success = false, ErrorMessage = "Số điện thoại này đã được đăng ký cho một tài khoản khác." };
            }
        }

        var account = new Account
        {
            Username = normalizedUsername,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = AccountRole.Customer,
            Status = AccountStatus.Active
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var customer = new Customer
        {
            AccountID = account.AccountID,
            FullName = cleanFullName,
            Phone = cleanPhone,
            Address = cleanAddress
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return new CustomerAuthResult
        {
            Success = true,
            Customer = new CustomerDto
            {
                CustomerId = customer.CustomerID,
                AccountId = account.AccountID,
                Username = account.Username,
                LastName = customer.LastName,
                FirstName = customer.FirstName,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Address = customer.Address
            }
        };
    }

    public async Task<bool> UpdateCustomerProfileAsync(UpdateCustomerProfileRequest request)
    {
        var customer = await _context.Customers.FindAsync(request.CustomerId);
        if (customer == null) return false;

        var cleanFullName = request.FullName?.Trim() ?? string.Empty;
        if (cleanFullName.Length < 2 || cleanFullName.Length > 100 || cleanFullName.Count(char.IsLetter) < 2)
            throw new InvalidOperationException("Họ và tên phải từ 2 đến 100 ký tự và chứa ít nhất 2 chữ cái.");

        var (isPhoneValid, cleanPhone, phoneError) = NormalizeAndValidatePhone(request.Phone, required: true);
        if (!isPhoneValid)
            throw new InvalidOperationException(phoneError);

        if (cleanPhone is not null)
        {
            var duplicatePhone = await _context.Customers.AnyAsync(
                c => c.Phone == cleanPhone && c.AccountID != null && c.CustomerID != request.CustomerId);
            if (duplicatePhone)
            {
                throw new InvalidOperationException($"Số điện thoại '{cleanPhone}' đã được đăng ký cho tài khoản khác.");
            }
        }

        var cleanAddress = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        if (cleanAddress is not null && (cleanAddress.Length < 5 || cleanAddress.Length > 255 || !cleanAddress.Any(char.IsLetter)))
            throw new InvalidOperationException("Địa chỉ giao bánh phải từ 5 đến 255 ký tự và chứa tên đường/khu vực hợp lệ.");

        customer.FullName = cleanFullName;
        customer.Phone = cleanPhone;
        customer.Address = cleanAddress;

        await _context.SaveChangesAsync();
        return true;
    }

    private static (bool IsValid, string? NormalizedPhone, string? ErrorMessage) NormalizeAndValidatePhone(string? rawPhone, bool required)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            return required
                ? (false, null, "Vui lòng nhập số điện thoại liên hệ.")
                : (true, null, null);
        }

        var trimmed = rawPhone.Trim();
        if (!Regex.IsMatch(trimmed, @"^0\d{8,10}$"))
        {
            return (false, null, "Số điện thoại không hợp lệ (chỉ nhập 9–11 chữ số, bắt đầu bằng số 0, VD: 0912345678).");
        }

        return (true, trimmed, null);
    }

    public async Task<CustomerDto?> GetCustomerProfileAsync(int customerId)
    {
        var customer = await _context.Customers
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.CustomerID == customerId);

        if (customer == null) return null;

        return new CustomerDto
        {
            CustomerId = customer.CustomerID,
            AccountId = customer.AccountID,
            Username = customer.Account?.Username ?? string.Empty,
            LastName = customer.LastName,
            FirstName = customer.FirstName,
            FullName = customer.FullName,
            Phone = customer.Phone,
            Address = customer.Address
        };
    }

    public async Task<CustomerDto?> GetCustomerByAccountIdAsync(int accountId)
    {
        var customer = await _context.Customers
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.AccountID == accountId);

        if (customer == null) return null;

        return new CustomerDto
        {
            CustomerId = customer.CustomerID,
            AccountId = customer.AccountID,
            Username = customer.Account?.Username ?? string.Empty,
            LastName = customer.LastName,
            FirstName = customer.FirstName,
            FullName = customer.FullName,
            Phone = customer.Phone,
            Address = customer.Address
        };
    }

    public async Task<string?> GetSessionStampAsync(int accountId, CancellationToken ct = default)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.AccountID == accountId)
            .Select(a => new { a.AccountID, a.Role, a.Status, a.Password })
            .FirstOrDefaultAsync(ct);

        if (account == null || account.Status == AccountStatus.Inactive)
        {
            return null;
        }

        return ComputeSessionStamp(account.AccountID, account.Role, account.Status, account.Password);
    }

    public async Task<bool> IsSessionValidAsync(
        int accountId,
        string? expectedRole,
        string? expectedStamp,
        CancellationToken ct = default)
    {
        if (accountId <= 0 || string.IsNullOrWhiteSpace(expectedRole) || string.IsNullOrWhiteSpace(expectedStamp))
        {
            return false;
        }

        var account = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.AccountID == accountId)
            .Select(a => new { a.AccountID, a.Role, a.Status, a.Password })
            .FirstOrDefaultAsync(ct);

        if (account == null || account.Status == AccountStatus.Inactive || account.Role != expectedRole)
        {
            return false;
        }

        var currentStamp = ComputeSessionStamp(account.AccountID, account.Role, account.Status, account.Password);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(currentStamp),
            Encoding.UTF8.GetBytes(expectedStamp));
    }

    private static string ComputeSessionStamp(int accountId, string role, string status, string passwordHash)
    {
        var payload = $"{accountId}:{role}:{status}:{passwordHash}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash)[..24];
    }
}


