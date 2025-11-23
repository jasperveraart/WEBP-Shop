namespace PWebShop.Infrastructure.Storage;

public sealed class ImageStorageOptions
{
    public string? PhysicalPath { get; set; }

    public string RequestPath { get; set; } = "/images";
}
