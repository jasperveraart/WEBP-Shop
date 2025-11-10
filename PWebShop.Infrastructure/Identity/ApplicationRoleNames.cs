namespace PWebShop.Infrastructure.Identity;

public static class ApplicationRoleNames
{
    public const string Customer = nameof(Customer);
    public const string Supplier = nameof(Supplier);
    public const string Employee = nameof(Employee);
    public const string Administrator = nameof(Administrator);

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Customer,
        Supplier,
        Employee,
        Administrator
    };
}
