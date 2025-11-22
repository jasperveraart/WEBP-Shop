using System;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Api.Dtos;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Api.Controllers;

[ApiController]
[Route("api/products/{productId:int}/images")]
public class ProductImagesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;

    private const string ActiveStatus = "Active";

    public ProductImagesController(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductImageDto>>> GetAll(int productId)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.Id, p.SupplierId, p.IsActive, p.Status })
            .FirstOrDefaultAsync();

        if (product is null)
        {
            return NotFound();
        }

        var currentUserId = GetCurrentUserId();
        if (!CanViewProduct(product.SupplierId, product.IsActive, product.Status, currentUserId))
        {
            return NotFound();
        }

        var images = await _db.ProductImages
            .AsNoTracking()
            .Where(img => img.ProductId == productId)
            .OrderByDescending(img => img.IsMain)
            .ThenBy(img => img.Id)
            .Select(img => new ProductImageDto
            {
                Id = img.Id,
                ProductId = img.ProductId,
                Url = img.Url,
                AltText = img.AltText,
                IsMain = img.IsMain
            })
            .ToListAsync();

        return Ok(images);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductImageDto>> GetById(int productId, int id)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.Id, p.SupplierId, p.IsActive, p.Status })
            .FirstOrDefaultAsync();

        if (product is null)
        {
            return NotFound();
        }

        var currentUserId = GetCurrentUserId();
        if (!CanViewProduct(product.SupplierId, product.IsActive, product.Status, currentUserId))
        {
            return NotFound();
        }

        var image = await _db.ProductImages
            .AsNoTracking()
            .Where(img => img.ProductId == productId && img.Id == id)
            .Select(img => new ProductImageDto
            {
                Id = img.Id,
                ProductId = img.ProductId,
                Url = img.Url,
                AltText = img.AltText,
                IsMain = img.IsMain
            })
            .FirstOrDefaultAsync();

        if (image is null)
        {
            return NotFound();
        }

        return Ok(image);
    }

    [HttpPost]
    [Authorize(Roles = ApplicationRoleNames.Supplier)]
    public async Task<ActionResult<ProductImageDto>> Create(int productId, [FromForm] ProductImageCreateDto dto)
    {
        var supplierId = GetCurrentUserId();
        if (!supplierId.HasValue)
        {
            return Forbid();
        }

        if (dto.File is null || dto.File.Length == 0)
        {
            return BadRequest("Een bestand is vereist voor het uploaden van een productafbeelding.");
        }

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
        {
            return NotFound($"Product with id {productId} does not exist.");
        }

        if (product.SupplierId != supplierId.Value)
        {
            return Forbid();
        }

        var url = await SaveImageAsync(dto.File, productId);

        var image = new ProductImage
        {
            ProductId = productId,
            Url = url,
            AltText = dto.AltText,
            IsMain = dto.IsMain
        };

        _db.ProductImages.Add(image);
        await _db.SaveChangesAsync();

        var created = await _db.ProductImages
            .AsNoTracking()
            .Where(img => img.Id == image.Id)
            .Select(img => new ProductImageDto
            {
                Id = img.Id,
                ProductId = img.ProductId,
                Url = img.Url,
                AltText = img.AltText,
                IsMain = img.IsMain
            })
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { productId, id = image.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = ApplicationRoleNames.Supplier)]
    public async Task<ActionResult<ProductImageDto>> Update(int productId, int id, [FromForm] ProductImageCreateDto dto)
    {
        var supplierId = GetCurrentUserId();
        if (!supplierId.HasValue)
        {
            return Forbid();
        }

        var image = await _db.ProductImages
            .Include(img => img.Product)
            .FirstOrDefaultAsync(img => img.ProductId == productId && img.Id == id);

        if (image is null)
        {
            return NotFound();
        }

        if (image.Product is null || image.Product.SupplierId != supplierId.Value)
        {
            return Forbid();
        }

        if (dto.File is not null && dto.File.Length > 0)
        {
            DeletePhysicalFile(image.Url);
            image.Url = await SaveImageAsync(dto.File, productId);
        }

        image.AltText = dto.AltText;
        image.IsMain = dto.IsMain;

        await _db.SaveChangesAsync();

        var updated = await _db.ProductImages
            .AsNoTracking()
            .Where(img => img.Id == image.Id)
            .Select(img => new ProductImageDto
            {
                Id = img.Id,
                ProductId = img.ProductId,
                Url = img.Url,
                AltText = img.AltText,
                IsMain = img.IsMain
            })
            .FirstAsync();

        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = ApplicationRoleNames.Supplier)]
    public async Task<IActionResult> Delete(int productId, int id)
    {
        var supplierId = GetCurrentUserId();
        if (!supplierId.HasValue)
        {
            return Forbid();
        }

        var image = await _db.ProductImages
            .Include(img => img.Product)
            .FirstOrDefaultAsync(img => img.ProductId == productId && img.Id == id);

        if (image is null)
        {
            return NotFound();
        }

        if (image.Product is null || image.Product.SupplierId != supplierId.Value)
        {
            return Forbid();
        }

        DeletePhysicalFile(image.Url);

        _db.ProductImages.Remove(image);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var parsed) ? parsed : null;
    }

    private bool CanViewProduct(int supplierId, bool isActive, string status, int? currentUserId)
    {
        if (isActive && string.Equals(status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (User.IsInRole(ApplicationRoleNames.Employee) || User.IsInRole(ApplicationRoleNames.Administrator))
        {
            return true;
        }

        return currentUserId.HasValue
            && User.IsInRole(ApplicationRoleNames.Supplier)
            && currentUserId.Value == supplierId;
    }

    private async Task<string> SaveImageAsync(IFormFile file, int productId)
    {
        var webRootPath = GetWebRootPath();
        var productFolder = Path.Combine(webRootPath, "images", "products", productId.ToString());
        Directory.CreateDirectory(productFolder);

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".bin";
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(productFolder, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        return $"/images/products/{productId}/{fileName}";
    }

    private void DeletePhysicalFile(string url)
    {
        var physicalPath = GetPhysicalPathFromUrl(url);
        if (physicalPath is not null && System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }
    }

    private string? GetPhysicalPathFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var webRootPath = GetWebRootPath();
        var relativePath = url.TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

        return Path.Combine(webRootPath, relativePath);
    }

    private string GetWebRootPath()
    {
        if (!string.IsNullOrWhiteSpace(_environment.WebRootPath))
        {
            return _environment.WebRootPath;
        }

        var fallbackPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(fallbackPath);
        return fallbackPath;
    }
}
