using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Domain.Entities;
using BakerySystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.Application.Features.Employees;

public sealed class EmployeeInput
{
    public int EmployeeID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Position { get; set; } = "Thu ngân";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Status { get; set; } = AccountStatus.Active;
}

public sealed class EmployeeService(IBakeryDbContext db)
{
    public Task<List<Employee>> GetAllAsync(CancellationToken ct = default) => db.Employees.AsNoTracking().Include(e => e.Account).OrderBy(e => e.FullName).ToListAsync(ct);

    public async Task SaveAsync(EmployeeInput input, CancellationToken ct = default)
    {
        input.FullName = input.FullName.Trim();
        input.Username = input.Username.Trim();
        if (input.FullName.Length == 0 || input.Username.Length == 0) throw new InvalidOperationException("Vui lòng nhập họ tên và tên đăng nhập.");
        if (input.Username.Length > 50) throw new InvalidOperationException("Tên đăng nhập không được vượt quá 50 ký tự.");
        if (input.Position is not ("Bếp bánh" or "Thu ngân" or "Quản lý" or "Giao hàng")) throw new InvalidOperationException("Vị trí nhân viên không hợp lệ.");
        input.Phone = input.Phone?.Trim();
        if (!string.IsNullOrWhiteSpace(input.Phone) &&
            (input.Phone.Length > 20 ||
             input.Phone.Any(character => !char.IsAsciiDigit(character) && !"+()-. ".Contains(character)) ||
             input.Phone.Count(char.IsAsciiDigit) is < 8 or > 15))
            throw new InvalidOperationException("Số điện thoại cần có từ 8 đến 15 chữ số; chỉ dùng số và các ký tự + ( ) - hoặc khoảng trắng, tối đa 20 ký tự.");
        if (await db.Accounts.AnyAsync(a => a.Username == input.Username && (input.EmployeeID == 0 || a.Employee == null || a.Employee.EmployeeID != input.EmployeeID), ct)) throw new InvalidOperationException("Tên đăng nhập đã được sử dụng.");

        if (input.EmployeeID == 0)
        {
            if (input.Password.Length < 6) throw new InvalidOperationException("Mật khẩu ban đầu cần ít nhất 6 ký tự.");
            var account = new Account { Username = input.Username, Password = BCrypt.Net.BCrypt.HashPassword(input.Password), Role = ResolveRole(input.Position), Status = AccountStatus.Active };
            db.Accounts.Add(account);
            db.Employees.Add(new Employee { Account = account, FullName = input.FullName, Phone = input.Phone?.Trim(), Position = input.Position });
        }
        else
        {
            var employee = await db.Employees.Include(e => e.Account).FirstOrDefaultAsync(e => e.EmployeeID == input.EmployeeID, ct) ?? throw new InvalidOperationException("Không tìm thấy nhân viên.");
            var wasActiveAdmin = employee.Account.Role == AccountRole.Admin && employee.Account.Status == AccountStatus.Active;
            var willRemainActiveAdmin = input.Position == "Quản lý" && input.Status != AccountStatus.Inactive;
            if (wasActiveAdmin && !willRemainActiveAdmin)
            {
                var anotherActiveAdminExists = await db.Accounts.AnyAsync(
                    account => account.AccountID != employee.AccountID
                        && account.Role == AccountRole.Admin
                        && account.Status == AccountStatus.Active,
                    ct);
                if (!anotherActiveAdminExists)
                    throw new InvalidOperationException("Không thể khóa hoặc hạ quyền Admin đang hoạt động cuối cùng. Hãy tạo/kích hoạt Admin khác trước.");
            }

            employee.FullName = input.FullName;
            employee.Phone = input.Phone?.Trim();
            employee.Position = input.Position;
            employee.Account.Username = input.Username;
            employee.Account.Role = ResolveRole(input.Position);
            employee.Account.Status = input.Status == AccountStatus.Inactive ? AccountStatus.Inactive : AccountStatus.Active;
            if (!string.IsNullOrWhiteSpace(input.Password))
            {
                if (input.Password.Length < 6) throw new InvalidOperationException("Mật khẩu cần ít nhất 6 ký tự.");
                employee.Account.Password = BCrypt.Net.BCrypt.HashPassword(input.Password);
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private static string ResolveRole(string position) => position switch
    {
        "Quản lý" => AccountRole.Admin,
        "Giao hàng" => AccountRole.Shipper,
        _ => AccountRole.Employee
    };

    public async Task<(Employee Employee, string Username)?> GetProfileAsync(int accountId, CancellationToken ct = default)
    {
        var employee = await db.Employees.AsNoTracking().Include(e => e.Account).FirstOrDefaultAsync(e => e.AccountID == accountId, ct);
        return employee is null ? null : (employee, employee.Account.Username);
    }

    public async Task UpdateProfileAsync(int accountId, string fullName, string? phone, CancellationToken ct = default)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.AccountID == accountId, ct) ?? throw new InvalidOperationException("Không tìm thấy hồ sơ nhân viên.");
        if (string.IsNullOrWhiteSpace(fullName)) throw new InvalidOperationException("Họ tên không được để trống.");
        employee.FullName = fullName.Trim(); employee.Phone = phone?.Trim();
        await db.SaveChangesAsync(ct);
    }
}
