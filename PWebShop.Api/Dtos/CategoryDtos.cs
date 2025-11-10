namespace PWebShop.Api.Dtos;

using System.Text.Json.Serialization;

public class CategoryDto
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }
}

public class CategoryCreateDto
{
    public int? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }
}

public class CategoryUpdateDto : CategoryCreateDto
{
}

public class CategoryTreeDto : CategoryDto
{
    [JsonPropertyOrder(99)]
    public List<CategoryTreeDto> Children { get; set; } = new();
}
