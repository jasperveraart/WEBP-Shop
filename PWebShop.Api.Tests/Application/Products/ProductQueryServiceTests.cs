using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using PWebShop.Api.Application.Products;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure.Identity;
using Xunit;

namespace PWebShop.Api.Tests.Application.Products;

public class ProductQueryServiceTests
{
    private readonly ProductQueryService _service = new();

    [Fact]
    public void ApplyVisibilityFilter_AnonymousOnlySeesApprovedActiveProducts()
    {
        var products = new List<Product>
        {
            new()
            {
                SupplierId = "supplier-1",
                Status = ProductStatus.PendingApproval,
                IsActive = false
            },
            new()
            {
                SupplierId = "supplier-2",
                Status = ProductStatus.Approved,
                IsActive = true,
                IsSuspendedBySupplier = false,
                IsListingOnly = false
            },
            new()
            {
                SupplierId = "supplier-3",
                Status = ProductStatus.Rejected,
                IsActive = false
            },
            new()
            {
                SupplierId = "supplier-4",
                Status = ProductStatus.Approved,
                IsActive = true,
                IsListingOnly = true
            }
        }.AsQueryable();

        var result = _service.ApplyVisibilityFilter(products, user: null, currentUserId: null).ToList();

        Assert.Single(result);
        Assert.All(result, p =>
        {
            Assert.Equal(ProductStatus.Approved, p.Status);
            Assert.True(p.IsActive);
        });
    }

    [Fact]
    public void ApplyVisibilityFilter_SupplierCanSeeOwnPendingProducts()
    {
        const string supplierId = "supplier-1";
        var user = CreateUserWithRole(ApplicationRoleNames.Supplier, supplierId);

        var products = new List<Product>
        {
            new()
            {
                SupplierId = supplierId,
                Status = ProductStatus.PendingApproval,
                IsActive = false
            },
            new()
            {
                SupplierId = "supplier-2",
                Status = ProductStatus.Approved,
                IsActive = true,
                IsListingOnly = false,
                IsSuspendedBySupplier = false
            }
        }.AsQueryable();

        var result = _service.ApplyVisibilityFilter(products, user, supplierId).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.SupplierId == supplierId && p.Status == ProductStatus.PendingApproval);
    }

    [Fact]
    public void ApplyVisibilityFilter_PrivilegedUsersSeeAllStatuses()
    {
        var user = CreateUserWithRole(ApplicationRoleNames.Employee, "employee-1");
        var products = new List<Product>
        {
            new()
            {
                SupplierId = "supplier-1",
                Status = ProductStatus.PendingApproval,
                IsActive = false
            },
            new()
            {
                SupplierId = "supplier-2",
                Status = ProductStatus.Rejected,
                IsActive = false
            },
            new()
            {
                SupplierId = "supplier-3",
                Status = ProductStatus.Approved,
                IsActive = true
            }
        }.AsQueryable();

        var result = _service.ApplyVisibilityFilter(products, user, "employee-1").ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Product_DefaultStatusIsPendingApproval()
    {
        var product = new Product();

        Assert.Equal(ProductStatus.PendingApproval, product.Status);
    }

    private static ClaimsPrincipal CreateUserWithRole(string role, string? userId)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (!string.IsNullOrWhiteSpace(userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId!));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
