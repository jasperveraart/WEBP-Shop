namespace PWebShop.Api.Dtos;

public class SubCategoryDto
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }
}

public class SubCategoryCreateDto
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }
}

public class SubCategoryUpdateDto : SubCategoryCreateDto
{
}
