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
[Route("api/supplier/products")]
[Authorize(Roles = ApplicationRoleNames.Supplier)]
public class SupplierProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SupplierProductsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPut("{productId:int}/stock")]
    public async Task<ActionResult<StockDto>> UpdateStock(int productId, StockUpdateDto dto)
    {
        var supplierId = GetCurrentSupplierId();
        if (!supplierId.HasValue)
        {
            return Forbid();
        }

        var product = await _db.Products
            .Include(p => p.Stock)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
        {
            return NotFound();
        }

        if (product.SupplierId != supplierId.Value)
        {
            return Forbid();
        }

        if (dto.QuantityAvailable < 0)
        {
            return BadRequest("Quantity cannot be negative.");
        }

        var now = DateTime.UtcNow;
        if (product.Stock is null)
        {
            product.Stock = new Stock
            {
                ProductId = product.Id,
                QuantityAvailable = dto.QuantityAvailable,
                LastUpdatedAt = now
            };
            _db.Stocks.Add(product.Stock);
        }
        else
        {
            product.Stock.QuantityAvailable = dto.QuantityAvailable;
            product.Stock.LastUpdatedAt = now;
        }

        await _db.SaveChangesAsync();

        var result = new StockDto
        {
            ProductId = product.Id,
            QuantityAvailable = product.Stock.QuantityAvailable,
            LastUpdatedAt = product.Stock.LastUpdatedAt
        };

        return Ok(result);
    }

    private int? GetCurrentSupplierId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var parsed) ? parsed : null;
    }
}
