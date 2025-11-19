using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Api.Application.Orders;
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
    private readonly IOrderWorkflow _orderWorkflow;

    public OrdersController(AppDbContext db, IOrderWorkflow orderWorkflow)
    {
        _db = db;
        _orderWorkflow = orderWorkflow;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(OrderCreateDto dto)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        if (dto is null)
        {
            return BadRequest("Order payload is required.");
        }

        var result = await _orderWorkflow.CreateOrderAsync(dto, customerId, HttpContext.RequestAborted);
        if (!result.Succeeded || result.Order is null)
        {
            return BadRequest(result.ErrorMessage);
        }

        return CreatedAtAction(nameof(GetOrderById), new { id = result.Order.Id }, MapOrder(result.Order));
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

        if (order.CustomerId != customerId)
        {
            return Forbid();
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

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
