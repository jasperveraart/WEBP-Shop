using System.ComponentModel.DataAnnotations;

namespace PWebShop.Api.Dtos;

public class PaymentSimulationRequestDto
{
    [Required]
    [MaxLength(100)]
    public string PaymentMethod { get; set; } = string.Empty;
}

public class PaymentDto
{
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? PaidAt { get; set; }
}
