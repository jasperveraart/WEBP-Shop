namespace PWebShop.Rcl.Dtos;

public class ProductSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public double CurrentPrice { get; set; }
    public int QuantityAvailable { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }
    public bool IsListingOnly { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? MainImageUrl { get; set; }
    public List<AvailabilityMethodDto> AvailabilityMethods { get; set; } = new();
}
