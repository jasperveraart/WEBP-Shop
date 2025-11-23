using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace PWebShop.Infrastructure.Storage;

public sealed class ImageStoragePathProvider
{
    private readonly IHostEnvironment _environment;
    private readonly ImageStorageOptions _options;
    private string? _cachedRootPath;

    public ImageStoragePathProvider(IHostEnvironment environment, IOptions<ImageStorageOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public string RequestPath => NormalizeRequestPath(_options.RequestPath);

    public string GetRootPath()
    {
        if (!string.IsNullOrWhiteSpace(_cachedRootPath))
        {
            return _cachedRootPath;
        }

        var configuredPath = _options.PhysicalPath;

        var rootPath = !string.IsNullOrWhiteSpace(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "shared-images"));

        Directory.CreateDirectory(rootPath);
        _cachedRootPath = rootPath;
        return rootPath;
    }

    public string GetProductFolder(int productId)
    {
        return Path.Combine(GetRootPath(), "products", productId.ToString());
    }

    public string BuildImageUrl(int productId, string fileName)
    {
        var requestPath = RequestPath.TrimEnd('/');
        return $"{requestPath}/products/{productId}/{fileName}";
    }

    public string? MapUrlToPhysicalPath(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var trimmedUrl = url.TrimStart('/');
        var requestPath = RequestPath.Trim('/');

        if (!string.IsNullOrWhiteSpace(requestPath))
        {
            if (trimmedUrl.StartsWith(requestPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                trimmedUrl = trimmedUrl[(requestPath.Length + 1)..];
            }
            else if (string.Equals(trimmedUrl, requestPath, StringComparison.OrdinalIgnoreCase))
            {
                trimmedUrl = string.Empty;
            }
        }

        var relativePath = trimmedUrl.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(GetRootPath(), relativePath);
    }

    private static string NormalizeRequestPath(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return "/images";
        }

        return rawPath.StartsWith("/") ? rawPath : $"/{rawPath}";
    }
}
