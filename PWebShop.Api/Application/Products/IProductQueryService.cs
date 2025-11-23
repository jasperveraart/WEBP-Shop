using System.Linq;
using System.Security.Claims;
using PWebShop.Domain.Entities;

namespace PWebShop.Api.Application.Products;

public interface IProductQueryService
{
    IQueryable<Product> ApplyVisibilityFilter(IQueryable<Product> query, ClaimsPrincipal? user, string? currentUserId);
}
