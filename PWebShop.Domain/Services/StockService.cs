namespace PWebShop.Domain.Services;

using PWebShop.Domain.Entities;

public interface IStockService
{
    void ReserveStockForOrder(Order order);

    void RestockProduct(Product product, int quantityToAdd);
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

        foreach (var line in order.OrderLines)
        {
            ValidateOrderLine(line);

            var product = line.Product!;
            var now = DateTime.UtcNow;
            var availableQuantity = ResolveAvailableQuantity(product);

            if (availableQuantity < line.Quantity)
            {
                var name = string.IsNullOrWhiteSpace(product.Name)
                    ? $"Product {product.Id}"
                    : product.Name;
                throw new InvalidOperationException($"Insufficient stock for product '{name}'.");
            }

            product.QuantityAvailable = availableQuantity - line.Quantity;
            SyncProductStock(product, now);
            product.UpdatedAt = now;
        }
    }

    public void RestockProduct(Product product, int quantityToAdd)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (quantityToAdd <= 0)
        {
            throw new InvalidOperationException("Restock quantity must be greater than zero.");
        }

        var now = DateTime.UtcNow;
        var currentQuantity = ResolveAvailableQuantity(product);

        try
        {
            product.QuantityAvailable = checked(currentQuantity + quantityToAdd);
        }
        catch (OverflowException ex)
        {
            throw new InvalidOperationException("Restock quantity results in an invalid stock level.", ex);
        }

        SyncProductStock(product, now);
        product.UpdatedAt = now;
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

    private static int ResolveAvailableQuantity(Product product)
    {
        if (product.QuantityAvailable <= 0 && product.Stock is not null && product.Stock.QuantityAvailable > 0)
        {
            product.QuantityAvailable = product.Stock.QuantityAvailable;
        }

        return product.QuantityAvailable;
    }

    private static void SyncProductStock(Product product, DateTime timestamp)
    {
        if (product.Stock is null)
        {
            product.Stock = new Stock
            {
                ProductId = product.Id,
                QuantityAvailable = product.QuantityAvailable,
                LastUpdatedAt = timestamp
            };
            return;
        }

        product.Stock.QuantityAvailable = product.QuantityAvailable;
        product.Stock.LastUpdatedAt = timestamp;
    }
}
