namespace PWebShop.Api.Dtos;

public class CatalogMenuDto
{
    public List<CatalogMenuCategoryDto> Categories { get; set; } = new();
}

public class CatalogMenuCategoryDto
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public List<CatalogMenuCategoryDto> Children { get; set; } = new();
}
