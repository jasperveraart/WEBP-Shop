namespace PWebShop.Domain.Entities;

public class Shipment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public string Carrier { get; set; } = string.Empty;

    public string? TrackingCode { get; set; }

    public ShipmentStatus Status { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }
}
