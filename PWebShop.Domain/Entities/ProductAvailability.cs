namespace PWebShop.Domain.Entities;

public class ProductAvailability
{
    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int AvailabilityMethodId { get; set; }

    public AvailabilityMethod? AvailabilityMethod { get; set; }
}
