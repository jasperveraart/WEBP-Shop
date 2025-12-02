using System;
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

namespace PWebShop.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IProductQueryService _productQueryService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        AppDbContext db,
        IProductQueryService productQueryService,
        ILogger<ProductsController> logger)
    {
        _db = db;
        _productQueryService = productQueryService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ProductSummaryDto>>> GetAll(
        [FromQuery] int? categoryId = null,
        [FromQuery] bool? isActive = null)
    {
        var currentUserId = GetCurrentUserId();

        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductAvailabilities)
                .ThenInclude(pa => pa.AvailabilityMethod)
            .Include(p => p.Images)
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

        var items = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                ShortDescription = p.ShortDescription,
                CurrentPrice = p.FinalPrice,
                QuantityAvailable = p.QuantityAvailable,
                IsFeatured = p.IsFeatured,
                IsActive = p.IsActive,
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

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = ApplicationRoleNames.Employee + "," + ApplicationRoleNames.Administrator)]
    public async Task<ActionResult<ProductDetailDto>> Approve(int id, ProductApprovalDto dto)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        product.Status = dto.Approve ? ProductStatus.Approved : ProductStatus.Rejected;
        product.IsActive = dto.Approve;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Product {ProductId} reviewed by {ReviewerId}: {Decision}. Note: {ReviewerNote}",
            product.Id,
            GetCurrentUserId() ?? "unknown",
            dto.Approve ? "approved" : "rejected",
            dto.ReviewerNote ?? "<none>");

        var reviewedProduct = await BuildDetailDtoQuery(User, GetCurrentUserId())
            .FirstAsync(p => p.Id == product.Id);

        return Ok(reviewedProduct);
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
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
}
