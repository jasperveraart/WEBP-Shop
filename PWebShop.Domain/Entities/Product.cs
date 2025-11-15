namespace PWebShop.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    public int SupplierId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string LongDescription { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<ProductAvailability> ProductAvailabilities { get; set; } = new();

    public List<ProductImage> Images { get; set; } = new();

    public List<Price> Prices { get; set; } = new();

    public Stock? Stock { get; set; }
}
