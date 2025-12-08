using PWebShop.Domain.Entities;
namespace PWebShop.Rcl.Services;

public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double Price { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; } = 1;

    // Keep Product for backward compatibility if needed, or remove if fully refactoring
    public Product? Product { get; set; }
}
