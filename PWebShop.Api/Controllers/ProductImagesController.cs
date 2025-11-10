using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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

    private const string ActiveStatus = "Active";

    public ProductImagesController(AppDbContext db)
    {
        _db = db;
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
            .OrderBy(img => img.SortOrder)
            .ThenByDescending(img => img.IsMain)
            .Select(img => new ProductImageDto
            {
                Id = img.Id,
                ProductId = img.ProductId,
                Url = img.Url,
                AltText = img.AltText,
                IsMain = img.IsMain,
                SortOrder = img.SortOrder
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
                IsMain = img.IsMain,
                SortOrder = img.SortOrder
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
    public async Task<ActionResult<ProductImageDto>> Create(int productId, ProductImageCreateDto dto)
    {
        var supplierId = GetCurrentUserId();
        if (!supplierId.HasValue)
        {
            return Forbid();
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

        var image = new ProductImage
        {
            ProductId = productId,
            Url = dto.Url,
            AltText = dto.AltText,
            IsMain = dto.IsMain,
            SortOrder = dto.SortOrder
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
                IsMain = img.IsMain,
                SortOrder = img.SortOrder
            })
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { productId, id = image.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = ApplicationRoleNames.Supplier)]
    public async Task<ActionResult<ProductImageDto>> Update(int productId, int id, ProductImageCreateDto dto)
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

        image.Url = dto.Url;
        image.AltText = dto.AltText;
        image.IsMain = dto.IsMain;
        image.SortOrder = dto.SortOrder;

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
                IsMain = img.IsMain,
                SortOrder = img.SortOrder
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
}
