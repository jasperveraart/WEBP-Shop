using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PWebShop.Api.Application.Products;
using PWebShop.Api.Dtos;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;
using PWebShop.Infrastructure.Storage;

namespace PWebShop.Api.Controllers;

[ApiController]
[Route("api/supplier/products")]
[Authorize(Roles = ApplicationRoleNames.Supplier)]
public class SupplierProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IProductQueryService _productQueryService;
    private readonly ILogger<SupplierProductsController> _logger;
    private readonly ImageStoragePathProvider _imageStoragePathProvider;

    public SupplierProductsController(
        AppDbContext db,
        IProductQueryService productQueryService,
        ILogger<SupplierProductsController> logger,
        ImageStoragePathProvider imageStoragePathProvider)
    {
        _db = db;
        _productQueryService = productQueryService;
        _logger = logger;
        _imageStoragePathProvider = imageStoragePathProvider;
    }

    [HttpGet]
    public async Task<ActionResult<List<SupplierProductSummaryDto>>> GetAll(
        [FromQuery] int? categoryId = null,
        [FromQuery] bool? isActive = null)
    {
        var supplierId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(supplierId))
        {
            return Forbid();
        }

        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductAvailabilities)
                .ThenInclude(pa => pa.AvailabilityMethod)
            .Include(p => p.Images)
            .Where(p => p.SupplierId == supplierId)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive);
        }

        var items = await query
            .OrderBy(p => p.Name)
            .Select(p => new SupplierProductSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                ShortDescription = p.ShortDescription,
                CurrentPrice = p.FinalPrice,
                QuantityAvailable = p.QuantityAvailable,
                MarkupPercentage = p.MarkupPercentage,
                BasePrice = p.BasePrice,
                IsFeatured = p.IsFeatured,
                IsActive = p.IsActive,
                Status = p.Status,
                IsListingOnly = p.IsListingOnly,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.DisplayName : null,
                MainImageUrl = p.Images.Where(i => i.IsMain).Select(i => i.Url).FirstOrDefault(),
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
                    .ToList()
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDetailDto>> GetById(int id)
    {
        var supplierId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(supplierId))
        {
            return Forbid();
        }

        var productQuery = BuildDetailDtoQuery(User, supplierId)
            .Where(p => p.SupplierId == supplierId)
            .Where(p => p.Id == id);

        var product = await productQuery.FirstOrDefaultAsync();

        if (product is null)
        {
            var exists = await _db.Products.AnyAsync(p => p.Id == id);
            if (exists)
            {
                return Forbid();
            }

            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDetailDto>> Create(ProductCreateDto dto)
    {
        var supplierId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(supplierId))
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

        // Markup is 0 initially, set by admin later if needed, or default
        double markupPercentage = 0;

        var product = new Product
        {
            CategoryId = dto.CategoryId,
            SupplierId = supplierId,
            Name = dto.Name,
            ShortDescription = dto.ShortDescription,
            LongDescription = dto.LongDescription,
            IsFeatured = false,
            IsActive = false,
            Status = ProductStatus.PendingApproval,
            BasePrice = dto.BasePrice,
            MarkupPercentage = markupPercentage,
            FinalPrice = CalculateFinalPrice(dto.BasePrice, markupPercentage),
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

        // Images are added separately via AddImage endpoint

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Product {ProductId} submitted by supplier {SupplierId} and requires review.",
            product.Id,
            supplierId);

        var created = await BuildDetailDtoQuery(User, supplierId)
            .FirstAsync(p => p.Id == product.Id);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDetailDto>> Update(int id, ProductUpdateDto dto)
    {
        var supplierId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(supplierId))
        {
            return Forbid();
        }

        var (product, failureResult) = await LoadProductForWriteAsync(
            id,
            supplierId,
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
        // MarkupPercentage is not updatable by supplier, preserve existing
        productEntity.FinalPrice = CalculateFinalPrice(dto.BasePrice, productEntity.MarkupPercentage);
        productEntity.IsListingOnly = dto.IsListingOnly;
        productEntity.IsActive = false;
        productEntity.Status = ProductStatus.PendingApproval;
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


        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Product {ProductId} updated by supplier {SupplierId} and set to pending approval.",
            productEntity.Id,
            supplierId);

        var updated = await BuildDetailDtoQuery(User, supplierId)
            .FirstAsync(p => p.Id == productEntity.Id);

        return Ok(updated);
    }

    [HttpPost("{id:int}/suspend")]
    public async Task<IActionResult> Suspend(int id)
    {
        var supplierId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(supplierId))
        {
            return Forbid();
        }

        var (product, failureResult) = await LoadProductForWriteAsync(id, supplierId);
        if (failureResult is not null)
        {
            return failureResult;
        }

        product!.IsSuspendedBySupplier = true;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:int}/unsuspend")]
    public async Task<IActionResult> Unsuspend(int id)
    {
        var supplierId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(supplierId))
        {
            return Forbid();
        }

        var (product, failureResult) = await LoadProductForWriteAsync(id, supplierId);
        if (failureResult is not null)
        {
            return failureResult;
        }

        product!.IsSuspendedBySupplier = false;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return NoContent();
    }


    [HttpPost("{id:int}/images")]
    public async Task<ActionResult<ProductImageDto>> AddImage(int id, [FromForm] ProductImageCreateDto dto)
    {
        var supplierId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(supplierId))
        {
            return Forbid();
        }

        if (dto.File is null || dto.File.Length == 0)
        {
            return BadRequest("A file is required to upload a product image.");
        }

        var (product, failureResult) = await LoadProductForWriteAsync(id, supplierId);
        if (failureResult is not null)
        {
            return failureResult;
        }

        var productEntity = product!;

        var url = await SaveImageAsync(dto.File, id);

        var image = new ProductImage
        {
            ProductId = id,
            Url = url,
            AltText = dto.AltText,
            IsMain = dto.IsMain
        };

        // If this is the first image or set as main, handle main image logic if needed
        if (dto.IsMain)
        {
            // Unset other main images
            var currentMain = await _db.ProductImages
                .Where(i => i.ProductId == id && i.IsMain)
                .ToListAsync();
            
            foreach (var cm in currentMain)
            {
                cm.IsMain = false;
            }
        }
        else
        {
            // If no main image exists, make this one main
            var hasMain = await _db.ProductImages.AnyAsync(i => i.ProductId == id && i.IsMain);
            if (!hasMain)
            {
                image.IsMain = true;
            }
        }

        _db.ProductImages.Add(image);
        
        // Reset approval status on modification
        productEntity.Status = ProductStatus.PendingApproval;
        productEntity.IsActive = false;
        productEntity.UpdatedAt = DateTime.UtcNow;

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

        return Ok(created);
    }

    [HttpPut("{id:int}/images/{imageId:int}/main")]
    public async Task<IActionResult> SetMainImage(int id, int imageId)
    {
        var supplierId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(supplierId))
        {
            return Forbid();
        }

        var (product, failureResult) = await LoadProductForWriteAsync(id, supplierId);
        if (failureResult is not null)
        {
            return failureResult;
        }

        var image = await _db.ProductImages
            .FirstOrDefaultAsync(img => img.ProductId == id && img.Id == imageId);

        if (image is null)
        {
            return NotFound("Image not found.");
        }

        var currentMainImages = await _db.ProductImages
            .Where(img => img.ProductId == id && img.IsMain)
            .ToListAsync();

        foreach (var main in currentMainImages)
        {
            main.IsMain = false;
        }

        image.IsMain = true;

        product!.Status = ProductStatus.PendingApproval;
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> RemoveImage(int id, int imageId)
    {
        var supplierId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(supplierId))
        {
            return Forbid();
        }

        var (product, failureResult) = await LoadProductForWriteAsync(id, supplierId);
        if (failureResult is not null)
        {
            return failureResult;
        }

        var image = await _db.ProductImages
            .FirstOrDefaultAsync(img => img.ProductId == id && img.Id == imageId);

        if (image is null)
        {
            return NotFound();
        }

        DeletePhysicalFile(image.Url);

        _db.ProductImages.Remove(image);

        // Reset approval status on modification
        product!.Status = ProductStatus.PendingApproval;
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private async Task<(Product? product, ActionResult? failureResult)> LoadProductForWriteAsync(
        int id,
        string supplierId,
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

        if (!string.Equals(product.SupplierId, supplierId, StringComparison.Ordinal))
        {
            return (null, Forbid());
        }

        return (product, null);
    }

    private IQueryable<ProductDetailDto> BuildDetailDtoQuery(ClaimsPrincipal user, string? currentUserId)
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
            .Select(p => new ProductDetailDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.DisplayName : null,
                SupplierId = p.SupplierId,
                Name = p.Name,
                ShortDescription = p.ShortDescription,
                LongDescription = p.LongDescription,
                IsFeatured = p.IsFeatured,
                IsActive = p.IsActive,
                Status = p.Status,
                IsListingOnly = p.IsListingOnly,
                IsSuspendedBySupplier = p.IsSuspendedBySupplier,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                BasePrice = p.BasePrice,
                MarkupPercentage = p.MarkupPercentage,
                CurrentPrice = p.FinalPrice,
                QuantityAvailable = p.QuantityAvailable,
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
                    .ToList()
            });
    }

    private static double CalculateFinalPrice(double basePrice, double markupPercentage)
    {
        var finalPrice = basePrice + (basePrice * markupPercentage / 100);
        return Math.Round(finalPrice, 2);
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
