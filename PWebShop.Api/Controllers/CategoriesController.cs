using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Api.Dtos;
using PWebShop.Infrastructure;

namespace PWebShop.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll([FromQuery] int? parentId = null)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.ParentId == parentId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.DisplayName)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                ParentId = c.ParentId,
                Name = c.Name,
                DisplayName = c.DisplayName,
                Description = c.Description,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive
            })
            .ToListAsync();

        return Ok(categories);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetById(int id)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                ParentId = c.ParentId,
                Name = c.Name,
                DisplayName = c.DisplayName,
                Description = c.Description,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync();

        if (category is null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [AllowAnonymous]
    [HttpGet("tree")]
    public async Task<ActionResult<IEnumerable<CategoryTreeDto>>> GetTree()
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.DisplayName)
            .ToListAsync();

        var lookup = categories.ToLookup(c => c.ParentId);

        List<CategoryTreeDto> BuildTree(int? parentId) => lookup[parentId]
            .Select(c => new CategoryTreeDto
            {
                Id = c.Id,
                ParentId = c.ParentId,
                Name = c.Name,
                DisplayName = c.DisplayName,
                Description = c.Description,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                Children = BuildTree(c.Id)
            })
            .ToList();

        var tree = BuildTree(null);

        return Ok(tree);
    }

}
