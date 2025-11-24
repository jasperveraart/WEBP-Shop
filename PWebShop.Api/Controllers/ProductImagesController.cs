using System;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Api.Dtos;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;
using PWebShop.Infrastructure.Storage;

namespace PWebShop.Api.Controllers;

[ApiController]
[Route("api/products/{productId:int}/images")]
public class ProductImagesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ImageStoragePathProvider _imageStoragePathProvider;

    public ProductImagesController(AppDbContext db, ImageStoragePathProvider imageStoragePathProvider)
    {
        _db = db;
        _imageStoragePathProvider = imageStoragePathProvider;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductImageDto>>> GetAll(int productId)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.Id, p.SupplierId, p.IsActive, p.IsSuspendedBySupplier, p.IsListingOnly, p.Status })
            .FirstOrDefaultAsync();

        if (product is null)
        {
            return NotFound();
        }

        var currentUserId = GetCurrentUserId();
        if (!CanViewProduct(product.SupplierId, product.IsActive, product.IsSuspendedBySupplier, product.IsListingOnly, product.Status, currentUserId))
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
            .Select(p => new { p.Id, p.SupplierId, p.IsActive, p.IsSuspendedBySupplier, p.IsListingOnly, p.Status })
            .FirstOrDefaultAsync();

        if (product is null)
        {
            return NotFound();
        }

        var currentUserId = GetCurrentUserId();
        if (!CanViewProduct(product.SupplierId, product.IsActive, product.IsSuspendedBySupplier, product.IsListingOnly, product.Status, currentUserId))
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
        if (string.IsNullOrWhiteSpace(supplierId))
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

        if (!string.Equals(product.SupplierId, supplierId, StringComparison.Ordinal))
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
        if (string.IsNullOrWhiteSpace(supplierId))
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

        if (image.Product is null || !string.Equals(image.Product.SupplierId, supplierId, StringComparison.Ordinal))
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
        if (string.IsNullOrWhiteSpace(supplierId))
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

        if (image.Product is null || !string.Equals(image.Product.SupplierId, supplierId, StringComparison.Ordinal))
        {
            return Forbid();
        }

        DeletePhysicalFile(image.Url);

        _db.ProductImages.Remove(image);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private bool CanViewProduct(
        string supplierId,
        bool isActive,
        bool isSuspendedBySupplier,
        bool isListingOnly,
        ProductStatus status,
        string? currentUserId)
    {
        if (User.IsInRole(ApplicationRoleNames.Employee) || User.IsInRole(ApplicationRoleNames.Administrator))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(currentUserId)
            && User.IsInRole(ApplicationRoleNames.Supplier)
            && string.Equals(currentUserId, supplierId, StringComparison.Ordinal))
        {
            return true;
        }

        var isApprovedAndActive = status == ProductStatus.Approved && isActive;
        return isApprovedAndActive && !isSuspendedBySupplier && !isListingOnly;
    }

    private async Task<string> SaveImageAsync(IFormFile file, int productId)
    {
        var productFolder = _imageStoragePathProvider.GetProductFolder(productId);
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

        return _imageStoragePathProvider.BuildImageUrl(productId, fileName);
    }

    private void DeletePhysicalFile(string url)
    {
        var physicalPath = _imageStoragePathProvider.MapUrlToPhysicalPath(url);
        if (physicalPath is not null && System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }
    }
}
