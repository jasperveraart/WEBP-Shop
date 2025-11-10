namespace PWebShop.Api.Dtos;

public class AdminUserSummaryDto
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

public class UpdateUserStatusRequestDto
{
    public string Action { get; set; } = string.Empty;
}

public class UpdateUserRolesRequestDto
{
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
}
