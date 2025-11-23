using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Api.Dtos;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Api.Controllers;

[ApiController]
[Route("api/supplier/orders")]
[Authorize(Roles = ApplicationRoleNames.Supplier)]
public class SupplierOrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public SupplierOrdersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SupplierOrderSummaryDto>>> GetOrders()
    {
        var supplierId = GetCurrentSupplierId();
        if (string.IsNullOrWhiteSpace(supplierId))
        {
            return Forbid();
        }

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.OrderLines.Any(ol => ol.Product != null && ol.Product.SupplierId == supplierId))
            .Select(o => new SupplierOrderSummaryDto
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                CustomerName = _db.Users
                    .Where(u => u.Id == o.CustomerId)
                    .Select(u => u.DisplayName ?? u.Email ?? u.UserName ?? string.Empty)
                    .FirstOrDefault() ?? string.Empty,
                ShippingAddress = o.ShippingAddress,
                TotalAmount = o.TotalAmount,
                Lines = o.OrderLines
                    .Where(ol => ol.Product != null && ol.Product.SupplierId == supplierId)
                    .Select(ol => new OrderLineDto
                    {
                        Id = ol.Id,
                        ProductId = ol.ProductId,
                        ProductName = ol.Product != null ? ol.Product.Name : string.Empty,
                        Quantity = ol.Quantity,
                        UnitPrice = ol.UnitPrice,
                        LineTotal = ol.LineTotal,
                        QuantityAvailable = ol.Product?.QuantityAvailable
                    })
                    .ToList()
            })
            .OrderBy(o => o.OrderDate)
            .ToListAsync();

        return Ok(orders);
    }

    private string? GetCurrentSupplierId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
