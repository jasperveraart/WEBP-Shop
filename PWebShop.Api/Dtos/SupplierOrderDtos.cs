namespace PWebShop.Api.Dtos;

public class SupplierOrderSummaryDto
{
    public int OrderId { get; set; }

    public DateTime OrderDate { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string ShippingAddress { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public List<OrderLineDto> Lines { get; set; } = new();
}
