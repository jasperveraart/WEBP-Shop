using System.ComponentModel.DataAnnotations;

namespace PWebShop.Rcl.Dtos;

public class PaymentSimulationRequestDto
{
    [Required]
    [MaxLength(100)]
    public string PaymentMethod { get; set; } = string.Empty;
}
