using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Api.Application.Products;
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
    private readonly IProductQueryService _productQueryService;

    public ProductsController(AppDbContext db, IProductQueryService productQueryService)
    {
        _db = db;
        _productQueryService = productQueryService;
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

        var currentUserId = GetCurrentUserId();

        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        query = _productQueryService.ApplyVisibilityFilter(query, User, currentUserId);

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
                    CurrentPrice = p.FinalPrice,
                    QuantityAvailable = p.Stock != null ? p.Stock.QuantityAvailable : 0,
                    Status = p.Status,
                    IsFeatured = p.IsFeatured,
                    IsActive = p.IsActive,
                    IsListingOnly = p.IsListingOnly,
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
        var currentUserId = GetCurrentUserId();

        var product = await BuildDetailDtoQuery(User, currentUserId)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

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
            Status = ProductStatusConstants.PendingApproval,
            IsFeatured = false,
            IsActive = false,
            BasePrice = dto.BasePrice,
            FinalPrice = 0,
            IsListingOnly = dto.IsListingOnly,
            IsSuspendedBySupplier = false,
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

        var created = await BuildDetailDtoQuery(User, supplierId)
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

        var (product, failureResult) = await LoadProductForWriteAsync(
            id,
            supplierId.Value,
            query => query
                .Include(p => p.ProductAvailabilities)
                .Include(p => p.Images));

        if (failureResult is not null)
        {
            return failureResult;
        }

        var productEntity = product!;

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

        productEntity.CategoryId = dto.CategoryId;
        productEntity.Name = dto.Name;
        productEntity.ShortDescription = dto.ShortDescription;
        productEntity.LongDescription = dto.LongDescription;
        productEntity.BasePrice = dto.BasePrice;
        productEntity.IsListingOnly = dto.IsListingOnly;
        productEntity.Status = ProductStatusConstants.PendingApproval;
        productEntity.IsActive = false;
        productEntity.UpdatedAt = DateTime.UtcNow;

        var existingAvailability = productEntity.ProductAvailabilities
            .Select(pa => pa.AvailabilityMethodId)
            .ToHashSet();

        var toRemoveAvailability = productEntity.ProductAvailabilities
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
                productEntity.ProductAvailabilities.Add(new ProductAvailability
                {
                    ProductId = productEntity.Id,
                    AvailabilityMethodId = availabilityId
                });
            }
        }

        var imageDtos = dto.Images ?? new();

        var incomingImageIds = imageDtos
            .Where(i => i.Id > 0)
            .Select(i => i.Id)
            .ToHashSet();

        var imagesToRemove = productEntity.Images
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
                var existingImage = productEntity.Images.FirstOrDefault(img => img.Id == imageDto.Id);
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
                productEntity.Images.Add(new ProductImage
                {
                    Url = imageDto.Url,
                    AltText = imageDto.AltText,
                    IsMain = imageDto.IsMain,
                    SortOrder = imageDto.SortOrder
                });
            }
        }

        await _db.SaveChangesAsync();

        var updated = await BuildDetailDtoQuery(User, supplierId)
            .FirstAsync(p => p.Id == productEntity.Id);

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

        var (product, failureResult) = await LoadProductForWriteAsync(id, supplierId.Value);

        if (failureResult is not null)
        {
            return failureResult;
        }

        var productEntity = product!;

        productEntity.IsActive = false;
        productEntity.Status = ProductStatusConstants.Inactive;
        productEntity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var parsed) ? parsed : null;
    }

    private async Task<(Product? product, IActionResult? failureResult)> LoadProductForWriteAsync(
        int id,
        int supplierId,
        Func<IQueryable<Product>, IQueryable<Product>>? configureQuery = null)
    {
        var query = _db.Products.AsQueryable();

        if (configureQuery is not null)
        {
            query = configureQuery(query);
        }

        var product = await query.FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return (null, NotFound());
        }

        if (product.SupplierId != supplierId)
        {
            return (null, Forbid());
        }

        return (product, null);
    }

    private IQueryable<ProductDetailDto> BuildDetailDtoQuery(ClaimsPrincipal user, int? currentUserId)
    {
        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductAvailabilities)
                .ThenInclude(pa => pa.AvailabilityMethod)
            .Include(p => p.Images)
            .AsQueryable();

        query = _productQueryService.ApplyVisibilityFilter(query, user, currentUserId);

        return query
            .Select(p => new
            {
                Product = p,
                LatestPriceWindow = p.Prices
                    .Where(price => price.IsCurrent)
                    .OrderByDescending(price => price.ValidFrom ?? DateTime.MinValue)
                    .Select(price => new
                    {
                        price.ValidFrom,
                        price.ValidTo
                    })
                    .FirstOrDefault()
            })
            .Select(x => new ProductDetailDto
            {
                Id = x.Product.Id,
                CategoryId = x.Product.CategoryId,
                CategoryName = x.Product.Category != null ? x.Product.Category.DisplayName : null,
                SupplierId = x.Product.SupplierId,
                Name = x.Product.Name,
                ShortDescription = x.Product.ShortDescription,
                LongDescription = x.Product.LongDescription,
                Status = x.Product.Status,
                IsFeatured = x.Product.IsFeatured,
                IsActive = x.Product.IsActive,
                IsListingOnly = x.Product.IsListingOnly,
                IsSuspendedBySupplier = x.Product.IsSuspendedBySupplier,
                CreatedAt = x.Product.CreatedAt,
                UpdatedAt = x.Product.UpdatedAt,
                BasePrice = x.Product.BasePrice,
                CurrentPrice = x.Product.FinalPrice,
                PriceValidFrom = x.LatestPriceWindow?.ValidFrom,
                PriceValidTo = x.LatestPriceWindow?.ValidTo,
                QuantityAvailable = x.Product.Stock != null ? x.Product.Stock.QuantityAvailable : 0,
                AvailabilityMethods = x.Product.ProductAvailabilities
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
                Images = x.Product.Images
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
