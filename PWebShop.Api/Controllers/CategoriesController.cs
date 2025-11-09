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
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll([FromQuery] bool includeSubCategories = false)
    {
        var query = _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.DisplayName)
            .AsQueryable();

        if (includeSubCategories)
        {
            query = query.Include(c => c.SubCategories);
        }

        var categories = await query
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                DisplayName = c.DisplayName,
                Description = c.Description,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                SubCategories = includeSubCategories
                    ? c.SubCategories
                        .OrderBy(sc => sc.SortOrder)
                        .ThenBy(sc => sc.DisplayName)
                        .Select(sc => new SubCategoryDto
                        {
                            Id = sc.Id,
                            CategoryId = sc.CategoryId,
                            CategoryName = c.DisplayName,
                            Name = sc.Name,
                            DisplayName = sc.DisplayName,
                            Description = sc.Description,
                            SortOrder = sc.SortOrder,
                            IsActive = sc.IsActive
                        })
                        .ToList()
                    : null
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetById(int id, [FromQuery] bool includeSubCategories = false)
    {
        var query = _db.Categories.AsNoTracking().Where(c => c.Id == id);

        if (includeSubCategories)
        {
            query = query.Include(c => c.SubCategories);
        }

        var category = await query
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                DisplayName = c.DisplayName,
                Description = c.Description,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                SubCategories = includeSubCategories
                    ? c.SubCategories
                        .OrderBy(sc => sc.SortOrder)
                        .ThenBy(sc => sc.DisplayName)
                        .Select(sc => new SubCategoryDto
                        {
                            Id = sc.Id,
                            CategoryId = sc.CategoryId,
                            CategoryName = c.DisplayName,
                            Name = sc.Name,
                            DisplayName = sc.DisplayName,
                            Description = sc.Description,
                            SortOrder = sc.SortOrder,
                            IsActive = sc.IsActive
                        })
                        .ToList()
                    : null
            })
            .FirstOrDefaultAsync();

        if (category is null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CategoryCreateDto dto)
    {
        var category = new Category
        {
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

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
