namespace PWebShop.Domain.Entities;

public class Category
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public Category? Parent { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public List<Category> Children { get; set; } = new();

    public List<Product> Products { get; set; } = new();
}
