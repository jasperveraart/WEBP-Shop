using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;
using PWebShop.Api.Dtos;

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
    public async Task<ActionResult<List<SupplierOrderDto>>> GetOrders()
    {
        var supplierId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(supplierId))
        {
            return Forbid();
        }

        // Fetch orders that have at least one product from this supplier
        var orders = await _db.Orders
            .Include(o => o.OrderLines)
            .ThenInclude(ol => ol.Product)
            .Where(o => o.OrderLines.Any(ol => ol.Product != null && ol.Product.SupplierId == supplierId))
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var supplierOrders = orders.Select(o =>
        {
            // Filter lines to only show this supplier's products
            var supplierLines = o.OrderLines
                .Where(ol => ol.Product != null && ol.Product.SupplierId == supplierId)
                .Select(ol => new SupplierOrderLineDto
                {
                    ProductId = ol.ProductId,
                    ProductName = ol.Product?.Name ?? "Unknown Product",
                    Quantity = ol.Quantity,
                    UnitPrice = ol.UnitPrice,
                    LineTotal = ol.LineTotal
                })
                .ToList();

            return new SupplierOrderDto
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                Status = o.Status,
                CustomerName = o.CustomerId, // Using ID for now as we might not have easy access to user name without UserManager
                TotalAmount = supplierLines.Sum(l => l.LineTotal),
                Lines = supplierLines
            };
        }).ToList();

        return Ok(supplierOrders);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] OrderStatus status)
    {
        var supplierId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(supplierId))
        {
            return Forbid();
        }

        // Ensure the order exists and contains products from this supplier
        var order = await _db.Orders
            .Include(o => o.OrderLines)
            .ThenInclude(ol => ol.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.OrderLines.Any(ol => ol.Product != null && ol.Product.SupplierId == supplierId));

        if (order is null)
        {
            return NotFound();
        }

        // Validate allowed status transitions if needed, for now just allow setting Shipped/Completed/Cancelled
        // The user specifically asked: "change status to Shipped or completed or cancelled"
        
        order.Status = status;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
