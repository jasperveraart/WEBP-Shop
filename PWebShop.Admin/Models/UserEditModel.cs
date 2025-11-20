using System.ComponentModel.DataAnnotations;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Admin.Models;

public class UserEditModel : IValidatableObject
{
    public string? Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    public string? Password { get; set; }

    public bool IsCustomer { get; set; }

    public bool IsSupplier { get; set; }

    public bool IsEmployee { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsNew => string.IsNullOrWhiteSpace(Id);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsNew && string.IsNullOrWhiteSpace(Password))
        {
            yield return new ValidationResult("Password is required for new users.", new[] { nameof(Password) });
        }
    }

    public List<string> GetSelectedRoles()
    {
        var roles = new List<string>();

        if (IsAdmin)
        {
            roles.Add(ApplicationRoleNames.Administrator);
        }

        if (IsEmployee)
        {
            roles.Add(ApplicationRoleNames.Employee);
        }

        if (IsCustomer)
        {
            roles.Add(ApplicationRoleNames.Customer);
        }

        if (IsSupplier)
        {
            roles.Add(ApplicationRoleNames.Supplier);
        }

        return roles;
    }
}
