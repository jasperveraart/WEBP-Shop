using System.ComponentModel.DataAnnotations;

namespace PWebShop.Api.Dtos;

public class PaymentSimulationRequestDto
{
    [Required]
    [MaxLength(100)]
    public string PaymentMethod { get; set; } = string.Empty;
}
