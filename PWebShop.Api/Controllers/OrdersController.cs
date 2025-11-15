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
[Route("api/orders")]
[Authorize(Roles = ApplicationRoleNames.Customer)]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(OrderCreateDto dto)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        if (dto.Items is null || dto.Items.Count == 0)
        {
            return BadRequest("Order must contain at least one item.");
        }

        var shippingAddress = await ResolveShippingAddressAsync(customerId, dto.ShippingAddress);
        if (string.IsNullOrWhiteSpace(shippingAddress))
        {
            return BadRequest("A shipping address is required.");
        }

        var itemGroups = dto.Items
            .GroupBy(i => i.ProductId)
            .Select(group => new OrderCreateItemDto
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();

        var productIds = itemGroups
            .Select(i => i.ProductId)
            .ToList();

        var products = await _db.Products
            .Include(p => p.Prices)
            .Include(p => p.Stock)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        if (products.Count != productIds.Count)
        {
            var missing = productIds.Except(products.Select(p => p.Id));
            return BadRequest($"Products not found: {string.Join(", ", missing)}");
        }

        var now = DateTime.UtcNow;

        foreach (var item in itemGroups)
        {
            var product = products.First(p => p.Id == item.ProductId);
            var currentPrice = product.Prices
                .Where(price => price.IsCurrent)
                .OrderByDescending(price => price.ValidFrom ?? DateTime.MinValue)
                .FirstOrDefault();

            if (currentPrice is null)
            {
                return BadRequest($"Product '{product.Name}' does not have a current price.");
            }

            if (product.Stock is null || product.Stock.QuantityAvailable < item.Quantity)
            {
                return BadRequest($"Insufficient stock for product '{product.Name}'.");
            }
        }

        var order = new Order
        {
            CustomerId = customerId,
            OrderDate = now,
            Status = OrderStatus.PendingPayment,
            PaymentStatus = PaymentStatus.Pending,
            ShippingAddress = shippingAddress,
            TotalAmount = 0m
        };

        foreach (var item in itemGroups)
        {
            var product = products.First(p => p.Id == item.ProductId);
            var currentPrice = product.Prices
                .Where(price => price.IsCurrent)
                .OrderByDescending(price => price.ValidFrom ?? DateTime.MinValue)
                .First();

            var unitPrice = currentPrice.FinalPrice;
            var lineTotal = unitPrice * item.Quantity;

            order.OrderLines.Add(new OrderLine
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                LineTotal = lineTotal
            });

            order.TotalAmount += lineTotal;
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var createdOrder = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == order.Id)
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.Product)
                    .ThenInclude(p => p.Stock)
            .Include(o => o.Payment)
            .Include(o => o.Shipment)
            .FirstAsync();

        return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, MapOrder(createdOrder));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.Product)
                    .ThenInclude(p => p.Stock)
            .Include(o => o.Payment)
            .Include(o => o.Shipment)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return Ok(orders.Select(MapOrder));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(int id)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var order = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == id && o.CustomerId == customerId)
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.Product)
                    .ThenInclude(p => p.Stock)
            .Include(o => o.Payment)
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync();

        if (order is null)
        {
            return NotFound();
        }

        return Ok(MapOrder(order));
    }

    [HttpPost("{orderId:int}/payments/simulate")]
    public async Task<ActionResult<OrderDto>> SimulatePayment(int orderId, PaymentSimulationRequestDto dto)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var order = await _db.Orders
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.Product)
                    .ThenInclude(p => p.Stock)
            .Include(o => o.Payment)
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);

        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.PendingPayment)
        {
            return BadRequest("Only orders that are pending payment can be paid.");
        }

        if (order.Payment is not null && order.Payment.Status == PaymentStatus.Succeeded)
        {
            return BadRequest("Payment already completed for this order.");
        }

        var now = DateTime.UtcNow;

        foreach (var line in order.OrderLines)
        {
            var stock = line.Product?.Stock;
            if (stock is null || stock.QuantityAvailable < line.Quantity)
            {
                var productName = line.Product?.Name ?? $"Product {line.ProductId}";
                return BadRequest($"Insufficient stock to complete payment for '{productName}'.");
            }
        }

        foreach (var line in order.OrderLines)
        {
            var stock = line.Product?.Stock;
            if (stock is not null)
            {
                stock.QuantityAvailable -= line.Quantity;
                stock.LastUpdatedAt = now;
            }
        }

        if (order.Payment is null)
        {
            order.Payment = new Payment
            {
                Amount = order.TotalAmount,
                PaymentMethod = dto.PaymentMethod,
                Status = PaymentStatus.Succeeded,
                PaidAt = now
            };

            _db.Payments.Add(order.Payment);
        }
        else
        {
            order.Payment.Amount = order.TotalAmount;
            order.Payment.PaymentMethod = dto.PaymentMethod;
            order.Payment.Status = PaymentStatus.Succeeded;
            order.Payment.PaidAt = now;
        }

        order.PaymentStatus = PaymentStatus.Succeeded;
        order.Status = OrderStatus.Paid;

        await _db.SaveChangesAsync();

        return Ok(MapOrder(order));
    }

    private static OrderDto MapOrder(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            Status = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            TotalAmount = order.TotalAmount,
            ShippingAddress = order.ShippingAddress,
            Lines = order.OrderLines
                .Select(line => new OrderLineDto
                {
                    Id = line.Id,
                    ProductId = line.ProductId,
                    ProductName = line.Product?.Name ?? string.Empty,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    LineTotal = line.LineTotal,
                    QuantityAvailable = line.Product?.Stock?.QuantityAvailable
                })
                .ToList(),
            Payment = order.Payment is null
                ? null
                : new PaymentDto
                {
                    Id = order.Payment.Id,
                    Amount = order.Payment.Amount,
                    PaymentMethod = order.Payment.PaymentMethod,
                    Status = order.Payment.Status.ToString(),
                    PaidAt = order.Payment.PaidAt
                },
            Shipment = order.Shipment is null
                ? null
                : new ShipmentDto
                {
                    Id = order.Shipment.Id,
                    Carrier = order.Shipment.Carrier,
                    TrackingCode = order.Shipment.TrackingCode,
                    Status = order.Shipment.Status.ToString(),
                    ShippedAt = order.Shipment.ShippedAt,
                    DeliveredAt = order.Shipment.DeliveredAt
                }
        };
    }

    private async Task<string?> ResolveShippingAddressAsync(string customerId, string? requestAddress)
    {
        if (!string.IsNullOrWhiteSpace(requestAddress))
        {
            return requestAddress.Trim();
        }

        return await _db.Users
            .Where(u => u.Id == customerId)
            .Select(u => u.DefaultShippingAddress)
            .FirstOrDefaultAsync();
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
