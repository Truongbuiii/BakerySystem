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
        if (string.IsNullOrWhiteSpace(request.FullName))
            return new CustomerAuthResult { Success = false, ErrorMessage = "Vui lòng nhập họ và tên của bạn." };

        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Trim().Length < 3)
            return new CustomerAuthResult { Success = false, ErrorMessage = "Tên đăng nhập phải có ít nhất 3 ký tự." };

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return new CustomerAuthResult { Success = false, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự." };

        if (request.Password != request.ConfirmPassword)
            return new CustomerAuthResult { Success = false, ErrorMessage = "Xác nhận mật khẩu không trùng khớp." };

        var normalizedUsername = request.Username.Trim();
        var existingAccount = await _context.Accounts
            .AnyAsync(a => a.Username == normalizedUsername);

        if (existingAccount)
        {
            return new CustomerAuthResult { Success = false, ErrorMessage = "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác." };
        }

        var cleanPhone = request.Phone?.Trim();
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
            FullName = request.FullName.Trim(),
            Phone = cleanPhone,
            Address = request.Address?.Trim()
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

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            customer.FullName = request.FullName.Trim();
        }
        customer.Phone = request.Phone?.Trim();
        customer.Address = request.Address?.Trim();

        await _context.SaveChangesAsync();
        return true;
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
}


