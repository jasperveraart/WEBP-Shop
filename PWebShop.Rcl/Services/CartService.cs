using PWebShop.Domain.Entities;

namespace PWebShop.Rcl.Services;

public class CartService
{
    public List<CartItem> Items { get; private set; } = new();

    public event Action? OnChange;

    public void AddToCart(Product product)
    {
        var existing = Items.FirstOrDefault(i => i.Product.Id == product.Id);

        if (existing is null)
            Items.Add(new CartItem { Product = product, Quantity = 1 });
        else
            existing.Quantity++;

        Notify();
    }

    public void Remove(Product product)
    {
        var item = Items.FirstOrDefault(i => i.Product.Id == product.Id);
        if (item != null)
            Items.Remove(item);

        Notify();
    }

    public void Update()
    {
        Notify();
    }

    private void Notify() => OnChange?.Invoke();
}