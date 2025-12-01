using Microsoft.AspNetCore.Http;

namespace PWebShop.Api.Dtos;

public class ProductImageDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string Url { get; set; } = string.Empty;

    public string AltText { get; set; } = string.Empty;

    public bool IsMain { get; set; }
}

public class ProductImageCreateDto
{
    public IFormFile? File { get; set; }

    public string AltText { get; set; } = string.Empty;

    public bool IsMain { get; set; }
}

public class ProductImageUpdateDto : ProductImageCreateDto
{
    public int Id { get; set; }
}
