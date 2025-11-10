using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Api.Dtos;
using PWebShop.Infrastructure;

namespace PWebShop.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly AppDbContext _db;

    public CatalogController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("menu")]
    public async Task<ActionResult<CatalogMenuDto>> GetMenu()
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.DisplayName)
            .ToListAsync();

        var lookup = categories.ToLookup(c => c.ParentId);

        CatalogMenuCategoryDto MapCategory(PWebShop.Domain.Entities.Category category) => new()
        {
            Id = category.Id,
            ParentId = category.ParentId,
            Name = category.Name,
            DisplayName = category.DisplayName,
            SortOrder = category.SortOrder,
            Children = lookup[category.Id]
                .Select(MapCategory)
                .ToList()
        };

        var dto = new CatalogMenuDto
        {
            Categories = lookup[null]
                .Select(MapCategory)
                .ToList()
        };

        return Ok(dto);
    }

    [HttpGet("featured")]
    public async Task<ActionResult<ProductDetailDto>> GetFeaturedProduct()
    {
        var featuredProduct = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsFeatured && p.IsActive)
            .OrderByDescending(p => p.UpdatedAt)
            .Include(p => p.Category)
            .Include(p => p.ProductAvailabilities)
                .ThenInclude(pa => pa.AvailabilityMethod)
            .Include(p => p.Images)
            .Select(p => new ProductDetailDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.DisplayName : null,
                SupplierId = p.SupplierId,
                Name = p.Name,
                ShortDescription = p.ShortDescription,
                LongDescription = p.LongDescription,
                BasePrice = p.BasePrice,
                MarkupPercentage = p.MarkupPercentage,
                FinalPrice = p.FinalPrice,
                Status = p.Status,
                IsFeatured = p.IsFeatured,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                AvailabilityMethods = p.ProductAvailabilities
                    .OrderBy(pa => pa.AvailabilityMethod != null ? pa.AvailabilityMethod.DisplayName : string.Empty)
                    .Select(pa => new AvailabilityMethodDto
                    {
                        Id = pa.AvailabilityMethodId,
                        Name = pa.AvailabilityMethod != null ? pa.AvailabilityMethod.Name : string.Empty,
                        DisplayName = pa.AvailabilityMethod != null ? pa.AvailabilityMethod.DisplayName : string.Empty,
                        Description = pa.AvailabilityMethod != null ? pa.AvailabilityMethod.Description : null,
                        IsActive = pa.AvailabilityMethod != null && pa.AvailabilityMethod.IsActive
                    })
                    .ToList(),
                Images = p.Images
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
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (featuredProduct is null)
        {
            return NotFound();
        }

        return Ok(featuredProduct);
    }
}
