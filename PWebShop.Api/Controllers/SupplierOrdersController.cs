using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Api.Dtos;
using PWebShop.Domain.Entities;
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

    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<SupplierOrderSummaryDto>>> GetPendingOrders()
    {
        var supplierId = GetCurrentSupplierId();
        if (!supplierId.HasValue)
        {
            return Forbid();
        }

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Paid)
            .Where(o => o.OrderLines.Any(ol => ol.Product != null && ol.Product.SupplierId == supplierId.Value))
            .Where(o => o.Shipment == null || o.Shipment.Status == ShipmentStatus.NotShipped)
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
                    .Where(ol => ol.Product != null && ol.Product.SupplierId == supplierId.Value)
                    .Select(ol => new OrderLineDto
                    {
                        Id = ol.Id,
                        ProductId = ol.ProductId,
                        ProductName = ol.Product != null ? ol.Product.Name : string.Empty,
                        Quantity = ol.Quantity,
                        UnitPrice = ol.UnitPrice,
                        LineTotal = ol.LineTotal,
                        QuantityAvailable = ol.Product != null && ol.Product.Stock != null
                            ? ol.Product.Stock.QuantityAvailable
                            : null
                    })
                    .ToList()
            })
            .OrderBy(o => o.OrderDate)
            .ToListAsync();

        return Ok(orders);
    }

    [HttpPost("{orderId:int}/shipment")]
    public async Task<ActionResult<ShipmentDto>> CreateShipment(int orderId, ShipmentCreateDto dto)
    {
        var supplierId = GetCurrentSupplierId();
        if (!supplierId.HasValue)
        {
            return Forbid();
        }

        var order = await _db.Orders
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.Product)
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
        {
            return NotFound();
        }

        if (!order.OrderLines.Any(ol => ol.Product != null && ol.Product.SupplierId == supplierId.Value))
        {
            return Forbid();
        }

        if (order.Status != OrderStatus.Paid)
        {
            return BadRequest("Only paid orders can be shipped.");
        }

        if (order.Shipment is not null)
        {
            return BadRequest("Shipment already exists for this order.");
        }

        var shipment = new Shipment
        {
            Carrier = dto.Carrier,
            TrackingCode = dto.TrackingCode,
            Status = ShipmentStatus.Shipped,
            ShippedAt = DateTime.UtcNow
        };

        order.Shipment = shipment;
        order.Status = OrderStatus.Shipped;

        _db.Shipments.Add(shipment);
        await _db.SaveChangesAsync();

        return Ok(MapShipment(shipment));
    }

    [HttpPost("{orderId:int}/shipment/delivered")]
    public async Task<ActionResult<ShipmentDto>> MarkShipmentDelivered(int orderId)
    {
        var supplierId = GetCurrentSupplierId();
        if (!supplierId.HasValue)
        {
            return Forbid();
        }

        var order = await _db.Orders
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.Product)
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
        {
            return NotFound();
        }

        if (!order.OrderLines.Any(ol => ol.Product != null && ol.Product.SupplierId == supplierId.Value))
        {
            return Forbid();
        }

        if (order.Shipment is null)
        {
            return BadRequest("Shipment has not been created for this order.");
        }

        order.Shipment.Status = ShipmentStatus.Delivered;
        order.Shipment.DeliveredAt = DateTime.UtcNow;
        order.Status = OrderStatus.Completed;

        await _db.SaveChangesAsync();

        return Ok(MapShipment(order.Shipment));
    }

    private static ShipmentDto MapShipment(Shipment shipment)
    {
        return new ShipmentDto
        {
            Id = shipment.Id,
            Carrier = shipment.Carrier,
            TrackingCode = shipment.TrackingCode,
            Status = shipment.Status.ToString(),
            ShippedAt = shipment.ShippedAt,
            DeliveredAt = shipment.DeliveredAt
        };
    }

    private int? GetCurrentSupplierId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var parsed) ? parsed : null;
    }
}
