using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Infrastructure;
using PWebShop.Api.Dtos;

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

    // GET api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        var products = await _db.Products
            .Include(p => p.Category)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                BasePrice = p.BasePrice,
                MarkupPercentage = p.MarkupPercentage,
                IsActive = p.IsActive,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty
            })
            .ToListAsync();

        return Ok(products);
    }
}