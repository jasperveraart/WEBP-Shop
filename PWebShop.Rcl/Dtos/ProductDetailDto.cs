using PWebShop.Domain.Entities;

namespace PWebShop.Rcl.Dtos;

public class ProductDetailDto
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string SupplierId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string LongDescription { get; set; } = string.Empty;

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; }

    public ProductStatus Status { get; set; }

    public bool IsListingOnly { get; set; }

    public bool IsSuspendedBySupplier { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public double BasePrice { get; set; }

    public double MarkupPercentage { get; set; }

    public double CurrentPrice { get; set; }

    public DateTime? PriceValidFrom { get; set; }

    public DateTime? PriceValidTo { get; set; }

    public int QuantityAvailable { get; set; }

    public List<AvailabilityMethodDto> AvailabilityMethods { get; set; } = new();

    public List<ProductImageDto> Images { get; set; } = new();
}

public class ProductImageDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsMain { get; set; }
}

