using PWebShop.Domain.Entities;

namespace PWebShop.Rcl.Services;

public class CartService
{
    public List<CartItem> Items { get; private set; } = new();

    public event Action? OnChange;

    public void AddToCart(Product product)
    {
        AddItem(new CartItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Price = product.FinalPrice,
            ImageUrl = product.Images.FirstOrDefault(i => i.IsMain)?.Url,
            Quantity = 1,
            Product = product
        });
    }

    public void AddItem(CartItem item)
    {
        var existing = Items.FirstOrDefault(i => i.ProductId == item.ProductId);

        if (existing is null)
            Items.Add(item);
        else
            existing.Quantity += item.Quantity;

        Notify();
    }

    public void Remove(Product product)
    {
        Remove(product.Id);
    }

    public void Remove(int productId)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
            Items.Remove(item);

        Notify();
    }

    public void Update()
    {
        Notify();
    }
    
    public void ClearCart()
    {
        Items = new List<CartItem>();
        Notify();
    }

    public int GetQuantity(int productId)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        return item?.Quantity ?? 0;
    }

    private void Notify() => OnChange?.Invoke();
}