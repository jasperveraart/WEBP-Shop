namespace PWebShop.Domain.Services;

using PWebShop.Domain.Entities;

public interface IStockService
{
    void ReserveStockForOrder(Order order);
}

public sealed class StockService : IStockService
{
    public void ReserveStockForOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.OrderLines.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;

        foreach (var line in order.OrderLines)
        {
            ValidateOrderLine(line);

            var product = line.Product!;
            var stock = product.Stock!;

            if (stock.QuantityAvailable < line.Quantity)
            {
                var name = string.IsNullOrWhiteSpace(product.Name)
                    ? $"Product {product.Id}"
                    : product.Name;
                throw new InvalidOperationException($"Insufficient stock for product '{name}'.");
            }

            stock.QuantityAvailable -= line.Quantity;
            stock.LastUpdatedAt = now;
        }
    }

    private static void ValidateOrderLine(OrderLine? line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (line.Quantity <= 0)
        {
            throw new InvalidOperationException("Order line quantity must be greater than zero.");
        }

        if (line.Product is null)
        {
            throw new InvalidOperationException("Order line is missing product information.");
        }

        if (line.Product.Stock is null)
        {
            throw new InvalidOperationException($"Stock is not configured for product '{line.Product.Name}'.");
        }
    }
}
