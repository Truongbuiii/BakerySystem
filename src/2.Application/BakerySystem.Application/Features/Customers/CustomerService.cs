using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BakerySystem.Application.Common.Interfaces;
using BakerySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BakerySystem.Application.Features.Customers;

public sealed class CustomerListItemDto
{
    public int CustomerId { get; set; }
    public string CustomerCode => $"#KH-{CustomerId:D4}";
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public int? AccountId { get; set; }
    public string? Username { get; set; }
    public string AccountStatus { get; set; } = "None"; // "Active", "Inactive", "None"
    public bool HasAppAccount => AccountId.HasValue && !string.IsNullOrWhiteSpace(Username);
    public bool HasPosOrders { get; set; }
    public bool HasAppOrders { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastOrderDate { get; set; }
}

public sealed class CustomerOrderHistoryDto
{
    public int OrderId { get; set; }
    public string OrderCode => $"#HD-{OrderId:D4}";
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
}

public sealed class CustomerDetailDto
{
    public int CustomerId { get; set; }
    public string CustomerCode => $"#KH-{CustomerId:D4}";
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public int? AccountId { get; set; }
    public string? Username { get; set; }
    public string AccountStatus { get; set; } = "None";
    public bool HasAppAccount => AccountId.HasValue && !string.IsNullOrWhiteSpace(Username);
    public bool HasPosOrders { get; set; }
    public bool HasAppOrders { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public List<CustomerOrderHistoryDto> RecentOrders { get; set; } = [];
}

public sealed class CustomerInputModel
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool CreateAccount { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string AccountStatus { get; set; } = "Active";
}

public sealed class CustomerService(IBakeryDbContext db)
{
    public async Task<List<CustomerListItemDto>> GetCustomersAsync(
        string? search = null,
        string? sourceFilter = null,
        string? statusFilter = null,
        CancellationToken ct = default)
    {
        var customers = await db.Customers.AsNoTracking()
            .Include(c => c.Account)
            .Include(c => c.Orders)
            .Where(c => (c.AccountID != null || (c.Phone != null && c.Phone.Trim() != ""))
                     && (c.AccountID != null || c.FirstName != "vãng lai"))
            .OrderByDescending(c => c.CustomerID)
            .ToListAsync(ct);

        var dtos = customers.Select(c =>
        {
            var validOrders = (c.Orders ?? Enumerable.Empty<Order>())
                .Where(o => !string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var accountStatus = c.Account != null ? (c.Account.Status ?? "None") : "None";
            var fullName = !string.IsNullOrWhiteSpace(c.FullName) ? c.FullName : (!string.IsNullOrWhiteSpace(c.FirstName) ? $"{c.LastName} {c.FirstName}".Trim() : "Khách hàng");
            var hasPosOrders = validOrders.Any(o => o.EmployeeID != null) || c.AccountID == null;
            var hasAppOrders = validOrders.Any(o => o.EmployeeID == null && c.AccountID != null);

            return new CustomerListItemDto
            {
                CustomerId = c.CustomerID,
                FullName = fullName,
                Phone = c.Phone,
                Address = c.Address,
                AccountId = c.AccountID,
                Username = c.Account?.Username,
                AccountStatus = accountStatus,
                HasPosOrders = hasPosOrders,
                HasAppOrders = hasAppOrders,
                TotalOrders = validOrders.Count,
                TotalSpent = validOrders.Sum(o => o.FinalAmount),
                LastOrderDate = validOrders.OrderByDescending(o => o.OrderDate).Select(o => (DateTime?)o.OrderDate).FirstOrDefault()
            };
        });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            dtos = dtos.Where(c =>
                c.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (c.Phone != null && c.Phone.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (c.Username != null && c.Username.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                c.CustomerCode.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(sourceFilter) && sourceFilter != "all")
        {
            if (sourceFilter == "app")
                dtos = dtos.Where(c => c.HasAppAccount);
            else if (sourceFilter == "pos")
                dtos = dtos.Where(c => c.HasPosOrders || !c.HasAppAccount);
            else if (sourceFilter == "guest")
                dtos = dtos.Where(c => !c.HasAppAccount);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
        {
            if (statusFilter == "active")
                dtos = dtos.Where(c => c.AccountStatus == "Active" || !c.HasAppAccount);
            else if (statusFilter == "inactive")
                dtos = dtos.Where(c => c.AccountStatus == "Inactive");
        }

        return dtos.ToList();
    }

    public async Task<CustomerDetailDto?> GetCustomerDetailAsync(int customerId, CancellationToken ct = default)
    {
        var customer = await db.Customers.AsNoTracking()
            .Include(c => c.Account)
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.CustomerID == customerId, ct);

        if (customer == null) return null;

        var validOrders = (customer.Orders ?? Enumerable.Empty<Order>())
            .OrderByDescending(o => o.OrderDate)
            .ToList();
        var fullName = !string.IsNullOrWhiteSpace(customer.FullName) ? customer.FullName : (!string.IsNullOrWhiteSpace(customer.FirstName) ? $"{customer.LastName} {customer.FirstName}".Trim() : "Khách hàng");
        var hasPosOrders = validOrders.Any(o => o.EmployeeID != null) || customer.AccountID == null;
        var hasAppOrders = validOrders.Any(o => o.EmployeeID == null && customer.AccountID != null);

        return new CustomerDetailDto
        {
            CustomerId = customer.CustomerID,
            LastName = customer.LastName ?? string.Empty,
            FirstName = customer.FirstName ?? string.Empty,
            FullName = fullName,
            Phone = customer.Phone,
            Address = customer.Address,
            AccountId = customer.AccountID,
            Username = customer.Account?.Username,
            AccountStatus = customer.Account?.Status ?? "None",
            HasPosOrders = hasPosOrders,
            HasAppOrders = hasAppOrders,
            TotalOrders = validOrders.Count(o => !string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)),
            TotalSpent = validOrders.Where(o => !string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)).Sum(o => o.FinalAmount),
            RecentOrders = validOrders.Take(10).Select(o => new CustomerOrderHistoryDto
            {
                OrderId = o.OrderID,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                FinalAmount = o.FinalAmount,
                DiscountAmount = o.DiscountAmount,
                Status = o.Status ?? string.Empty,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus
            }).ToList()
        };
    }

    public async Task<int> SaveCustomerAsync(CustomerInputModel input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.FullName))
            throw new InvalidOperationException("Vui lòng nhập họ và tên khách hàng.");

        Customer? customer;

        if (input.CustomerId == 0)
        {
            // Kiểm tra trùng số điện thoại
            if (!string.IsNullOrWhiteSpace(input.Phone))
            {
                var cleanPhone = input.Phone.Trim();
                var exists = await db.Customers.AnyAsync(c => c.Phone == cleanPhone, ct);
                if (exists)
                    throw new InvalidOperationException($"Số điện thoại '{cleanPhone}' đã được đăng ký cho khách hàng khác.");
            }

            customer = new Customer();
            customer.FullName = input.FullName.Trim();
            customer.Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();
            customer.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();

            // Nếu người dùng chọn tạo tài khoản App
            if (input.CreateAccount && !string.IsNullOrWhiteSpace(input.Username))
            {
                var userClean = input.Username.Trim();
                var userExists = await db.Accounts.AnyAsync(a => a.Username == userClean, ct);
                if (userExists)
                    throw new InvalidOperationException($"Tên đăng nhập '{userClean}' đã tồn tại.");

                var account = new Account
                {
                    Username = userClean,
                    Password = string.IsNullOrWhiteSpace(input.Password) ? "123456" : input.Password,
                    Role = "Customer",
                    Status = input.AccountStatus
                };

                customer.Account = account;
            }

            db.Customers.Add(customer);
        }
        else
        {
            customer = await db.Customers
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.CustomerID == input.CustomerId, ct);

            if (customer == null)
                throw new InvalidOperationException("Không tìm thấy thông tin khách hàng cần cập nhật.");

            // Kiểm tra trùng SĐT với người khác
            if (!string.IsNullOrWhiteSpace(input.Phone))
            {
                var cleanPhone = input.Phone.Trim();
                var exists = await db.Customers.AnyAsync(c => c.Phone == cleanPhone && c.CustomerID != input.CustomerId, ct);
                if (exists)
                    throw new InvalidOperationException($"Số điện thoại '{cleanPhone}' đã thuộc về khách hàng khác.");
            }

            customer.FullName = input.FullName.Trim();
            customer.Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();
            customer.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();

            if (customer.Account != null)
            {
                customer.Account.Status = input.AccountStatus;
                if (!string.IsNullOrWhiteSpace(input.Password))
                {
                    customer.Account.Password = input.Password;
                }
            }
            else if (input.CreateAccount && !string.IsNullOrWhiteSpace(input.Username))
            {
                var userClean = input.Username.Trim();
                var userExists = await db.Accounts.AnyAsync(a => a.Username == userClean, ct);
                if (userExists)
                    throw new InvalidOperationException($"Tên đăng nhập '{userClean}' đã tồn tại.");

                var account = new Account
                {
                    Username = userClean,
                    Password = string.IsNullOrWhiteSpace(input.Password) ? "123456" : input.Password,
                    Role = "Customer",
                    Status = input.AccountStatus
                };

                customer.Account = account;
            }
        }

        await db.SaveChangesAsync(ct);
        return customer.CustomerID;
    }

    public async Task ToggleAccountStatusAsync(int customerId, CancellationToken ct = default)
    {
        var customer = await db.Customers
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.CustomerID == customerId, ct);

        if (customer?.Account == null)
            throw new InvalidOperationException("Khách hàng này chưa có tài khoản đăng nhập trên hệ thống.");

        customer.Account.Status = customer.Account.Status == "Active" ? "Inactive" : "Active";
        await db.SaveChangesAsync(ct);
    }
}
