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
            .Include(c => c.SubCategories.Where(sc => sc.IsActive))
            .ToListAsync();

        var dto = new CatalogMenuDto
        {
            Categories = categories
                .Select(c => new CatalogMenuCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    DisplayName = c.DisplayName,
                    SortOrder = c.SortOrder,
                    SubCategories = c.SubCategories
                        .OrderBy(sc => sc.SortOrder)
                        .ThenBy(sc => sc.DisplayName)
                        .Select(sc => new CatalogMenuSubCategoryDto
                        {
                            Id = sc.Id,
                            Name = sc.Name,
                            DisplayName = sc.DisplayName,
                            SortOrder = sc.SortOrder
                        })
                        .ToList()
                })
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
            .Include(p => p.SubCategory)!
            .ThenInclude(sc => sc.Category)
            .Include(p => p.ProductAvailabilities)
                .ThenInclude(pa => pa.AvailabilityMethod)
            .Include(p => p.Images)
            .Select(p => new ProductDetailDto
            {
                Id = p.Id,
                SubCategoryId = p.SubCategoryId,
                SubCategoryName = p.SubCategory != null ? p.SubCategory.DisplayName : null,
                CategoryId = p.SubCategory != null ? p.SubCategory.CategoryId : 0,
                CategoryName = p.SubCategory != null && p.SubCategory.Category != null
                    ? p.SubCategory.Category.DisplayName
                    : null,
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
