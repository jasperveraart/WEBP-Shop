namespace PWebShop.Api.Dtos;

public class ProductSummaryDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public decimal? FinalPrice { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; }

    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }
}

public class ProductDetailDto
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }

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

    public List<AvailabilityMethodDto> AvailabilityMethods { get; set; } = new();

    public List<ProductImageDto> Images { get; set; } = new();
}

public class ProductCreateDto
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string LongDescription { get; set; } = string.Empty;

    public List<int> AvailabilityMethodIds { get; set; } = new();

    public List<ProductImageCreateDto> Images { get; set; } = new();
}

public class ProductUpdateDto
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string LongDescription { get; set; } = string.Empty;

    public List<int> AvailabilityMethodIds { get; set; } = new();

    public List<ProductImageUpdateDto> Images { get; set; } = new();
}
