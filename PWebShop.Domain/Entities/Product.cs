using PWebShop.Infrastructure.Identity;

namespace PWebShop.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    public string SupplierId { get; set; } = string.Empty;

    public ApplicationUser? Supplier { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string LongDescription { get; set; } = string.Empty;

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; }

    public int QuantityAvailable { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public double BasePrice { get; set; } = 0.0;

    public double MarkupPercentage { get; set; } = 0.0;

    public double FinalPrice { get; set; } = 0.0;

    public bool IsListingOnly { get; set; } = false;

    public bool IsSuspendedBySupplier { get; set; } = false;

    public List<ProductAvailability> ProductAvailabilities { get; set; } = new();

    public List<ProductImage> Images { get; set; } = new();
}
