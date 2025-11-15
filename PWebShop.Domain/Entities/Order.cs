namespace PWebShop.Domain.Entities;

public class Order
{
    public int Id { get; set; }

    public string CustomerId { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public decimal TotalAmount { get; set; }

    public string ShippingAddress { get; set; } = string.Empty;

    public List<OrderLine> OrderLines { get; set; } = new();

    public Payment? Payment { get; set; }

    public Shipment? Shipment { get; set; }
}
