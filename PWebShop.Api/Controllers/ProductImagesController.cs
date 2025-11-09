using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Api.Dtos;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure;

namespace PWebShop.Api.Controllers;

[ApiController]
[Route("api/products/{productId:int}/images")]
public class ProductImagesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductImagesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductImageDto>>> GetAll(int productId)
    {
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
    public async Task<ActionResult<ProductImageDto>> GetById(int productId, int id)
    {
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
    public async Task<ActionResult<ProductImageDto>> Create(int productId, ProductImageCreateDto dto)
    {
        var productExists = await _db.Products.AnyAsync(p => p.Id == productId);
        if (!productExists)
        {
            return NotFound($"Product with id {productId} does not exist.");
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
    public async Task<ActionResult<ProductImageDto>> Update(int productId, int id, ProductImageCreateDto dto)
    {
        var image = await _db.ProductImages
            .FirstOrDefaultAsync(img => img.ProductId == productId && img.Id == id);

        if (image is null)
        {
            return NotFound();
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
    public async Task<IActionResult> Delete(int productId, int id)
    {
        var image = await _db.ProductImages
            .FirstOrDefaultAsync(img => img.ProductId == productId && img.Id == id);

        if (image is null)
        {
            return NotFound();
        }

        _db.ProductImages.Remove(image);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
