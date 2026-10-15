namespace BakerySystem.Application.Features.Auth;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? FullName { get; set; }
}

public class ChangePasswordRequest
{
    public int AccountId { get; set; }
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class CustomerRegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class CustomerDto
{
    public int CustomerId { get; set; }
    public int? AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class CustomerAuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public CustomerDto? Customer { get; set; }
}

public class UpdateCustomerProfileRequest
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

