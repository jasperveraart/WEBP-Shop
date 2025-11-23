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
    }

    private static int ResolveAvailableQuantity(Product product)
    {
        return product.QuantityAvailable;
    }
}
