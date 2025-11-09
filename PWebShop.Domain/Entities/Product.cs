namespace PWebShop.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal BasePrice { get; set; }

    public decimal MarkupPercentage { get; set; }

    public bool IsActive { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}