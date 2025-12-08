using PWebShop.Domain.Entities;

namespace PWebShop.Rcl.Dtos;

public class SupplierProductSummaryDto : ProductSummaryDto
{
    public ProductStatus Status { get; set; }

    public double BasePrice { get; set; }

    public double MarkupPercentage { get; set; }
}
