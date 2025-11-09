namespace PWebShop.Domain.Entities;

public class AvailabilityMethod
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public List<ProductAvailability> ProductAvailabilities { get; set; } = new();
}
