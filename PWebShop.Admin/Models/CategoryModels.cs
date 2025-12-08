using System.ComponentModel.DataAnnotations;

namespace PWebShop.Admin.Models;

public class CategoryEditModel
{
    public int? Id { get; set; }

    public int? ParentId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;
}

public class CategoryTreeItem
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpanded { get; set; } = true;
    public List<CategoryTreeItem> Children { get; set; } = new();
}

public enum DropPosition
{
    Before,
    After,
    Child
}
