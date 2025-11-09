using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Api.Dtos;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure;

namespace PWebShop.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ProductSummaryDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? categoryId = null,
        [FromQuery] int? subCategoryId = null,
        [FromQuery] bool? isActive = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.SubCategory)!
            .ThenInclude(sc => sc.Category)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.SubCategory != null && p.SubCategory.CategoryId == categoryId);
        }

        if (subCategoryId.HasValue)
        {
            query = query.Where(p => p.SubCategoryId == subCategoryId);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                ShortDescription = p.ShortDescription,
                FinalPrice = p.FinalPrice,
                Status = p.Status,
                IsFeatured = p.IsFeatured,
                IsActive = p.IsActive,
                SubCategoryId = p.SubCategoryId,
                SubCategoryName = p.SubCategory != null ? p.SubCategory.DisplayName : null,
                CategoryId = p.SubCategory != null ? p.SubCategory.CategoryId : 0,
                CategoryName = p.SubCategory != null && p.SubCategory.Category != null
                    ? p.SubCategory.Category.DisplayName
                    : null
            })
            .ToListAsync();

        var result = new PagedResultDto<ProductSummaryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDetailDto>> GetById(int id)
    {
        var product = await BuildDetailDtoQuery()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDetailDto>> Create(ProductCreateDto dto)
    {
        var subCategory = await _db.SubCategories
            .FirstOrDefaultAsync(sc => sc.Id == dto.SubCategoryId);
        if (subCategory is null)
        {
            return BadRequest($"SubCategory with id {dto.SubCategoryId} does not exist.");
        }

        var distinctAvailabilityIds = dto.AvailabilityMethodIds
            .Distinct()
            .ToList();

        if (distinctAvailabilityIds.Any())
        {
            var existingAvailabilityIds = await _db.AvailabilityMethods
                .Where(am => distinctAvailabilityIds.Contains(am.Id))
                .Select(am => am.Id)
                .ToListAsync();

            var missing = distinctAvailabilityIds.Except(existingAvailabilityIds).ToList();
            if (missing.Any())
            {
                return BadRequest($"Availability methods not found: {string.Join(", ", missing)}");
            }
        }

        var product = new Product
        {
            SubCategoryId = dto.SubCategoryId,
            SupplierId = dto.SupplierId,
            Name = dto.Name,
            ShortDescription = dto.ShortDescription,
            LongDescription = dto.LongDescription,
            BasePrice = dto.BasePrice,
            MarkupPercentage = dto.MarkupPercentage,
            FinalPrice = dto.FinalPrice ?? CalculateFinalPrice(dto.BasePrice, dto.MarkupPercentage),
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "PendingApproval" : dto.Status,
            IsFeatured = dto.IsFeatured,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var availabilityId in distinctAvailabilityIds)
        {
            product.ProductAvailabilities.Add(new ProductAvailability
            {
                AvailabilityMethodId = availabilityId
            });
        }

        foreach (var imageDto in dto.Images)
        {
            product.Images.Add(new ProductImage
            {
                Url = imageDto.Url,
                AltText = imageDto.AltText,
                IsMain = imageDto.IsMain,
                SortOrder = imageDto.SortOrder
            });
        }

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var created = await BuildDetailDtoQuery()
            .FirstAsync(p => p.Id == product.Id);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDetailDto>> Update(int id, ProductUpdateDto dto)
    {
        var product = await _db.Products
            .Include(p => p.ProductAvailabilities)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        var subCategoryExists = await _db.SubCategories
            .AnyAsync(sc => sc.Id == dto.SubCategoryId);
        if (!subCategoryExists)
        {
            return BadRequest($"SubCategory with id {dto.SubCategoryId} does not exist.");
        }

        var distinctAvailabilityIds = dto.AvailabilityMethodIds
            .Distinct()
            .ToList();

        if (distinctAvailabilityIds.Any())
        {
            var existingAvailabilityIds = await _db.AvailabilityMethods
                .Where(am => distinctAvailabilityIds.Contains(am.Id))
                .Select(am => am.Id)
                .ToListAsync();

            var missing = distinctAvailabilityIds.Except(existingAvailabilityIds).ToList();
            if (missing.Any())
            {
                return BadRequest($"Availability methods not found: {string.Join(", ", missing)}");
            }
        }

        product.SubCategoryId = dto.SubCategoryId;
        product.SupplierId = dto.SupplierId;
        product.Name = dto.Name;
        product.ShortDescription = dto.ShortDescription;
        product.LongDescription = dto.LongDescription;
        product.BasePrice = dto.BasePrice;
        product.MarkupPercentage = dto.MarkupPercentage;
        product.FinalPrice = dto.FinalPrice ?? CalculateFinalPrice(dto.BasePrice, dto.MarkupPercentage);
        product.Status = string.IsNullOrWhiteSpace(dto.Status) ? product.Status : dto.Status;
        product.IsFeatured = dto.IsFeatured;
        product.IsActive = dto.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        var existingAvailability = product.ProductAvailabilities
            .Select(pa => pa.AvailabilityMethodId)
            .ToHashSet();

        var toRemoveAvailability = product.ProductAvailabilities
            .Where(pa => !distinctAvailabilityIds.Contains(pa.AvailabilityMethodId))
            .ToList();
        if (toRemoveAvailability.Any())
        {
            _db.ProductAvailabilities.RemoveRange(toRemoveAvailability);
        }

        foreach (var availabilityId in distinctAvailabilityIds)
        {
            if (!existingAvailability.Contains(availabilityId))
            {
                product.ProductAvailabilities.Add(new ProductAvailability
                {
                    ProductId = product.Id,
                    AvailabilityMethodId = availabilityId
                });
            }
        }

        var incomingImageIds = dto.Images
            .Where(i => i.Id > 0)
            .Select(i => i.Id)
            .ToHashSet();

        var imagesToRemove = product.Images
            .Where(img => !incomingImageIds.Contains(img.Id))
            .ToList();
        if (imagesToRemove.Any())
        {
            _db.ProductImages.RemoveRange(imagesToRemove);
        }

        foreach (var imageDto in dto.Images)
        {
            if (imageDto.Id > 0)
            {
                var existingImage = product.Images.FirstOrDefault(img => img.Id == imageDto.Id);
                if (existingImage is not null)
                {
                    existingImage.Url = imageDto.Url;
                    existingImage.AltText = imageDto.AltText;
                    existingImage.IsMain = imageDto.IsMain;
                    existingImage.SortOrder = imageDto.SortOrder;
                }
            }
            else
            {
                product.Images.Add(new ProductImage
                {
                    Url = imageDto.Url,
                    AltText = imageDto.AltText,
                    IsMain = imageDto.IsMain,
                    SortOrder = imageDto.SortOrder
                });
            }
        }

        await _db.SaveChangesAsync();

        var updated = await BuildDetailDtoQuery()
            .FirstAsync(p => p.Id == product.Id);

        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static decimal CalculateFinalPrice(decimal basePrice, decimal markupPercentage)
    {
        return Math.Round(basePrice + (basePrice * markupPercentage / 100m), 2);
    }

    private IQueryable<ProductDetailDto> BuildDetailDtoQuery()
    {
        return _db.Products
            .AsNoTracking()
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
            });
    }
}
