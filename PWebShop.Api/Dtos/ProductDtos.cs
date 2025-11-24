namespace PWebShop.Api.Dtos;

public class ProductSummaryDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public double CurrentPrice { get; set; }

    public int QuantityAvailable { get; set; }

    public double MarkupPercentage { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; }

    public PWebShop.Domain.Entities.ProductStatus Status { get; set; }

    public bool IsListingOnly { get; set; }

    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }
}

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

    public PWebShop.Domain.Entities.ProductStatus Status { get; set; }

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

public class ProductCreateDto
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string LongDescription { get; set; } = string.Empty;

    public double BasePrice { get; set; }

    public double MarkupPercentage { get; set; }

    public bool IsListingOnly { get; set; }

    public List<int> AvailabilityMethodIds { get; set; } = new();

    public List<ProductImageCreateDto> Images { get; set; } = new();
}

public class ProductUpdateDto
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string LongDescription { get; set; } = string.Empty;

    public double BasePrice { get; set; }

    public double MarkupPercentage { get; set; }

    public bool IsListingOnly { get; set; }

    public List<int> AvailabilityMethodIds { get; set; } = new();

    public List<ProductImageUpdateDto> Images { get; set; } = new();
}

public class ProductApprovalDto
{
    public bool Approve { get; set; }

    public string? ReviewerNote { get; set; }
}
