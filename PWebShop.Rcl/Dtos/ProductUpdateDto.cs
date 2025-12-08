namespace PWebShop.Rcl.Dtos;

public class ProductUpdateDto
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string LongDescription { get; set; } = string.Empty;

    public double BasePrice { get; set; }

    public bool IsListingOnly { get; set; }

    public List<int> AvailabilityMethodIds { get; set; } = new();
}
