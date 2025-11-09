namespace PWebShop.Api.Dtos;

public class AvailabilityMethodDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}

public class AvailabilityMethodCreateDto
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}

public class AvailabilityMethodUpdateDto : AvailabilityMethodCreateDto
{
}
