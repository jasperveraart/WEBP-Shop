namespace PWebShop.Domain.Entities;

public class Stock
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int QuantityAvailable { get; set; }

    public DateTime LastUpdatedAt { get; set; }
}
