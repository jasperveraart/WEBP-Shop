namespace PWebShop.Domain.Services;

using PWebShop.Domain.Entities;

public interface IStockService
{
    void IncreaseStock(Product product, int quantity);

    void DecreaseStock(Product product, int quantity);
}

public sealed class StockService : IStockService
{
    public void IncreaseStock(Product product, int quantity)
    {
        ValidateProduct(product);
        ValidateQuantity(quantity);

        product.QuantityAvailable += quantity;
        SynchronizeStockSnapshot(product);
    }

    public void DecreaseStock(Product product, int quantity)
    {
        ValidateProduct(product);
        ValidateQuantity(quantity);

        if (product.QuantityAvailable < quantity)
        {
            throw new InvalidOperationException("Insufficient stock to fulfill the request.");
        }

        product.QuantityAvailable -= quantity;
        SynchronizeStockSnapshot(product);
    }

    private static void ValidateProduct(Product? product)
    {
        ArgumentNullException.ThrowIfNull(product);
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }
    }

    private static void SynchronizeStockSnapshot(Product product)
    {
        var now = DateTime.UtcNow;

        if (product.Stock is null)
        {
            product.Stock = new Stock
            {
                ProductId = product.Id,
                QuantityAvailable = product.QuantityAvailable,
                LastUpdatedAt = now
            };
            return;
        }

        product.Stock.QuantityAvailable = product.QuantityAvailable;
        product.Stock.LastUpdatedAt = now;
    }
}
