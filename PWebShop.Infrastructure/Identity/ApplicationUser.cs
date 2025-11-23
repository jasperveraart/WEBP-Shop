using Microsoft.AspNetCore.Identity;

namespace PWebShop.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

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
}
