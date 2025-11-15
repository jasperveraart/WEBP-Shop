namespace PWebShop.Domain.Entities;

public class Price
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public decimal BasePrice { get; set; }

    public decimal MarkupPercentage { get; set; }

    public decimal FinalPrice { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool IsCurrent { get; set; }
}
