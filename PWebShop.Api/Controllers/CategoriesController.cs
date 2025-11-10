using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Api.Dtos;
using PWebShop.Domain.Entities;
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

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CategoryCreateDto dto)
    {
        if (dto.ParentId.HasValue)
        {
            var parentExists = await _db.Categories.AnyAsync(c => c.Id == dto.ParentId.Value);
            if (!parentExists)
            {
                return BadRequest($"Parent category with id {dto.ParentId.Value} does not exist.");
            }
        }

        var category = new Category
        {
            ParentId = dto.ParentId,
            Name = dto.Name,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var created = await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == category.Id)
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
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryUpdateDto dto)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        if (dto.ParentId == id)
        {
            return BadRequest("A category cannot be its own parent.");
        }

        if (dto.ParentId.HasValue)
        {
            var parentExists = await _db.Categories.AnyAsync(c => c.Id == dto.ParentId.Value);
            if (!parentExists)
            {
                return BadRequest($"Parent category with id {dto.ParentId.Value} does not exist.");
            }

            var ancestorId = dto.ParentId;
            while (ancestorId.HasValue)
            {
                if (ancestorId.Value == id)
                {
                    return BadRequest("A category cannot be moved under one of its descendants.");
                }

                ancestorId = await _db.Categories
                    .Where(c => c.Id == ancestorId.Value)
                    .Select(c => c.ParentId)
                    .FirstOrDefaultAsync();
            }
        }

        category.ParentId = dto.ParentId;
        category.Name = dto.Name;
        category.DisplayName = dto.DisplayName;
        category.Description = dto.Description;
        category.SortOrder = dto.SortOrder;
        category.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();

        var updated = await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == category.Id)
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
            .FirstAsync();

        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        var hasChildren = await _db.Categories.AnyAsync(c => c.ParentId == id);
        if (hasChildren)
        {
            return BadRequest("Cannot delete a category that has child categories.");
        }

        var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
        {
            return BadRequest("Cannot delete a category that contains products.");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
