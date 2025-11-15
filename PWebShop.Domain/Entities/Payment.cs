namespace PWebShop.Domain.Entities;

public class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public PaymentStatus Status { get; set; }

    public DateTime? PaidAt { get; set; }
}
