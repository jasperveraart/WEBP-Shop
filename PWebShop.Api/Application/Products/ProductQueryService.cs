using System.Linq;
using System.Security.Claims;
using PWebShop.Infrastructure.Identity;
using PWebShop.Domain.Entities;

namespace PWebShop.Api.Application.Products;

public class ProductQueryService : IProductQueryService
{
    public IQueryable<Product> ApplyVisibilityFilter(IQueryable<Product> query, ClaimsPrincipal? user, string? currentUserId)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (UserCanViewAllProducts(user))
        {
            return query;
        }

        if (UserIsSupplier(user) && !string.IsNullOrWhiteSpace(currentUserId))
        {
            var supplierId = currentUserId;
            return query.Where(p =>
                p.SupplierId == supplierId ||
                (p.Status == ProductStatus.Approved
                    && p.IsActive
                    && !p.IsSuspendedBySupplier
                    && !p.IsListingOnly));
        }

        return query.Where(p =>
            p.Status == ProductStatus.Approved
            && p.IsActive
            && !p.IsSuspendedBySupplier
            && !p.IsListingOnly);
    }

    private static bool UserIsSupplier(ClaimsPrincipal? user) => user?.IsInRole(ApplicationRoleNames.Supplier) == true;

    private static bool UserCanViewAllProducts(ClaimsPrincipal? user)
    {
        return user?.IsInRole(ApplicationRoleNames.Employee) == true
            || user?.IsInRole(ApplicationRoleNames.Administrator) == true;
    }
}
