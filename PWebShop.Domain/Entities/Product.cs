namespace PWebShop.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    public int SubCategoryId { get; set; }

    public SubCategory? SubCategory { get; set; }

    public int SupplierId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string LongDescription { get; set; } = string.Empty;

    public decimal BasePrice { get; set; }

    public decimal MarkupPercentage { get; set; }

    public decimal? FinalPrice { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<ProductAvailability> ProductAvailabilities { get; set; } = new();

    public List<ProductImage> Images { get; set; } = new();
}
