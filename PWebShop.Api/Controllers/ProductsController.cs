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
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    private const string ActiveStatus = "Active";
    private const string PendingApprovalStatus = "PendingApproval";
    private const string InactiveStatus = "Inactive";

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResultDto<ProductSummaryDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? categoryId = null,
        [FromQuery] bool? isActive = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (!UserCanViewNonPublicProducts())
        {
            query = query.Where(p => p.IsActive && p.Status == ActiveStatus);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId);
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
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.DisplayName : null
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
    [AllowAnonymous]
    public async Task<ActionResult<ProductDetailDto>> GetById(int id)
    {
        var productInfo = await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { p.Id, p.SupplierId, p.IsActive, p.Status })
            .FirstOrDefaultAsync();

        if (productInfo is null)
        {
            return NotFound();
        }

        var currentUserId = GetCurrentUserId();
        if (!CanViewProduct(productInfo.SupplierId, productInfo.IsActive, productInfo.Status, currentUserId))
        {
            return NotFound();
        }

        var product = await BuildDetailDtoQuery()
            .FirstAsync(p => p.Id == id);

        return Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = ApplicationRoleNames.Supplier)]
    public async Task<ActionResult<ProductDetailDto>> Create(ProductCreateDto dto)
    {
        var supplierId = GetCurrentUserId();
        if (!supplierId.HasValue)
        {
            return Forbid();
        }

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == dto.CategoryId);
        if (category is null)
        {
            return BadRequest($"Category with id {dto.CategoryId} does not exist.");
        }

        var availabilityIds = dto.AvailabilityMethodIds ?? new();

        var distinctAvailabilityIds = availabilityIds
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
            CategoryId = dto.CategoryId,
            SupplierId = supplierId.Value,
            Name = dto.Name,
            ShortDescription = dto.ShortDescription,
            LongDescription = dto.LongDescription,
            BasePrice = 0m,
            MarkupPercentage = 0m,
            FinalPrice = null,
            Status = PendingApprovalStatus,
            IsFeatured = false,
            IsActive = false,
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

        var imageDtos = dto.Images ?? new();

        foreach (var imageDto in imageDtos)
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
    [Authorize(Roles = ApplicationRoleNames.Supplier)]
    public async Task<ActionResult<ProductDetailDto>> Update(int id, ProductUpdateDto dto)
    {
        var supplierId = GetCurrentUserId();
        if (!supplierId.HasValue)
        {
            return Forbid();
        }

        var product = await _db.Products
            .Include(p => p.ProductAvailabilities)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        if (product.SupplierId != supplierId.Value)
        {
            return Forbid();
        }

        var categoryExists = await _db.Categories
            .AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists)
        {
            return BadRequest($"Category with id {dto.CategoryId} does not exist.");
        }

        var availabilityIds = dto.AvailabilityMethodIds ?? new();

        var distinctAvailabilityIds = availabilityIds
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

        product.CategoryId = dto.CategoryId;
        product.Name = dto.Name;
        product.ShortDescription = dto.ShortDescription;
        product.LongDescription = dto.LongDescription;
        product.Status = PendingApprovalStatus;
        product.IsActive = false;
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

        var imageDtos = dto.Images ?? new();

        var incomingImageIds = imageDtos
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

        foreach (var imageDto in imageDtos)
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
    [Authorize(Roles = ApplicationRoleNames.Supplier)]
    public async Task<IActionResult> Delete(int id)
    {
        var supplierId = GetCurrentUserId();
        if (!supplierId.HasValue)
        {
            return Forbid();
        }

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        if (product.SupplierId != supplierId.Value)
        {
            return Forbid();
        }

        product.IsActive = false;
        product.Status = InactiveStatus;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var parsed) ? parsed : null;
    }

    private bool UserCanViewNonPublicProducts()
    {
        return User.IsInRole(ApplicationRoleNames.Employee)
            || User.IsInRole(ApplicationRoleNames.Administrator);
    }

    private bool CanViewProduct(int supplierId, bool isActive, string status, int? currentUserId)
    {
        if (isActive && string.Equals(status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (UserCanViewNonPublicProducts())
        {
            return true;
        }

        return currentUserId.HasValue
            && User.IsInRole(ApplicationRoleNames.Supplier)
            && currentUserId.Value == supplierId;
    }

    private IQueryable<ProductDetailDto> BuildDetailDtoQuery()
    {
        return _db.Products
            .AsNoTracking()
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
            });
    }
}
