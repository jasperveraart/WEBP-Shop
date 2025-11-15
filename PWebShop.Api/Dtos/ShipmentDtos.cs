using System.ComponentModel.DataAnnotations;

namespace PWebShop.Api.Dtos;

public class ShipmentCreateDto
{
    [Required]
    [MaxLength(200)]
    public string Carrier { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TrackingCode { get; set; }
}

public class ShipmentDto
{
    public int Id { get; set; }

    public string Carrier { get; set; } = string.Empty;

    public string? TrackingCode { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }
}
