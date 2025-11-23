using System.ComponentModel.DataAnnotations;

namespace PWebShop.Api.Dtos;

public class OrderCreateDto
{
    [Required]
    [MinLength(1)]
    public List<OrderCreateItemDto> Items { get; set; } = new();

    public string? ShippingAddress { get; set; }
}

public class OrderCreateItemDto
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }

    public DateTime OrderDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public string ShippingAddress { get; set; } = string.Empty;

    public List<OrderLineDto> Lines { get; set; } = new();
}

public class OrderLineDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public int? QuantityAvailable { get; set; }
}
