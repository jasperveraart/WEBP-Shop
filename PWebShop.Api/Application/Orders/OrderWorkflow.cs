using Microsoft.EntityFrameworkCore;
using System.Linq;
using PWebShop.Api.Dtos;
using PWebShop.Domain.Entities;
using PWebShop.Domain.Services;
using PWebShop.Infrastructure;

namespace PWebShop.Api.Application.Orders;

public interface IOrderWorkflow
{
    Task<OrderCreationResult> CreateOrderAsync(OrderCreateDto dto, string customerId, CancellationToken cancellationToken);
}

public sealed class OrderWorkflow : IOrderWorkflow
{
    private readonly AppDbContext _db;
    private readonly IStockService _stockService;

    public OrderWorkflow(AppDbContext db, IStockService stockService)
    {
        _db = db;
        _stockService = stockService;
    }

    public async Task<OrderCreationResult> CreateOrderAsync(OrderCreateDto dto, string customerId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);

        if (dto.Items is null || dto.Items.Count == 0)
        {
            return OrderCreationResult.Failure("Order must contain at least one item.");
        }

        var shippingAddress = await ResolveShippingAddressAsync(customerId, dto.ShippingAddress, cancellationToken);
        if (string.IsNullOrWhiteSpace(shippingAddress))
        {
            return OrderCreationResult.Failure("A shipping address is required.");
        }

        var consolidatedItems = dto.Items
            .GroupBy(i => i.ProductId)
            .Select(group => new OrderCreateItemDto
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();

        var productIds = consolidatedItems
            .Select(i => i.ProductId)
            .ToList();

        var products = await _db.Products
            .Include(p => p.Prices)
            .Include(p => p.Stock)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Count)
        {
            var missing = productIds.Except(products.Select(p => p.Id));
            return OrderCreationResult.Failure($"Products not available for purchase: {string.Join(", ", missing)}.");
        }

        var productsById = products.ToDictionary(p => p.Id);

        var now = DateTime.UtcNow;

        var order = new Order
        {
            CustomerId = customerId,
            OrderDate = now,
            Status = OrderStatus.PendingPayment,
            PaymentStatus = PaymentStatus.Pending,
            ShippingAddress = shippingAddress,
            TotalAmount = 0m
        };

        foreach (var item in consolidatedItems)
        {
            if (!productsById.TryGetValue(item.ProductId, out var product))
            {
                return OrderCreationResult.Failure($"Product '{item.ProductId}' is not available for purchase.");
            }

            var availabilityValidationMessage = ValidateProductForOrder(product);
            if (availabilityValidationMessage is not null)
            {
                return OrderCreationResult.Failure(availabilityValidationMessage);
            }

            var currentPrice = product.Prices
                .Where(price => price.IsCurrent)
                .OrderByDescending(price => price.ValidFrom ?? DateTime.MinValue)
                .FirstOrDefault();

            if (currentPrice is null)
            {
                return OrderCreationResult.Failure($"No current price configured for product '{product.Name}'.");
            }

            var unitPrice = currentPrice.FinalPrice;
            var lineTotal = unitPrice * item.Quantity;

            order.OrderLines.Add(new OrderLine
            {
                ProductId = product.Id,
                Product = product,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                LineTotal = lineTotal
            });

            order.TotalAmount += lineTotal;
        }

        try
        {
            _stockService.ReserveStockForOrder(order);
        }
        catch (InvalidOperationException ex)
        {
            return OrderCreationResult.Failure(ex.Message);
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        var createdOrder = await LoadCreatedOrderAsync(order.Id, cancellationToken);
        return OrderCreationResult.Success(createdOrder);
    }

    private async Task<Order> LoadCreatedOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.Product)
                    .ThenInclude(p => p.Stock)
            .Include(o => o.Payment)
            .Include(o => o.Shipment)
            .FirstAsync(cancellationToken);
    }

    private async Task<string?> ResolveShippingAddressAsync(string customerId, string? requestAddress, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestAddress))
        {
            return requestAddress.Trim();
        }

        return await _db.Users
            .Where(u => u.Id == customerId)
            .Select(u => u.DefaultShippingAddress)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string? ValidateProductForOrder(Product product)
    {
        var productName = string.IsNullOrWhiteSpace(product.Name)
            ? $"Product {product.Id}"
            : product.Name;

        if (product.IsListingOnly)
        {
            return $"Product '{productName}' is not available for purchase because it is listing-only.";
        }

        if (product.IsSuspendedBySupplier)
        {
            return $"Product '{productName}' is not available for purchase because it has been suspended by the supplier.";
        }

        return null;
    }
}
