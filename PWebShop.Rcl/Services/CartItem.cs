using PWebShop.Domain.Entities;
namespace PWebShop.Rcl.Services;

public class CartItem
{
    public Product Product { get; set; } = default!;
    public int Quantity { get; set; } = 1;
}
