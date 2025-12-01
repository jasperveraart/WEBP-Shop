namespace PWebShop.Api.Dtos;

public class RegisterCustomerRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? DefaultShippingAddress { get; set; }
}

public class RegisterSupplierRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string VatNumber { get; set; } = string.Empty;
}

public class AuthResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequestDto
{
    public string DisplayName { get; set; } = string.Empty;

    public string? DefaultShippingAddress { get; set; }

    public string? CompanyName { get; set; }

    public string? VatNumber { get; set; }
}

public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
}

public class CurrentUserResponseDto
{
    public string UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsCustomer { get; set; }

    public bool IsSupplier { get; set; }

    public bool IsEmployee { get; set; }

    public bool IsAdministrator { get; set; }

    public bool IsActive { get; set; }

    public bool IsPendingApproval { get; set; }

    public bool IsBlocked { get; set; }

    public string? DefaultShippingAddress { get; set; }

    public string? CompanyName { get; set; }

    public string? VatNumber { get; set; }

    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
}
