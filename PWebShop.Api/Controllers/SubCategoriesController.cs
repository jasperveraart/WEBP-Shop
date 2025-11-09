using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Api.Dtos;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure;

namespace PWebShop.Api.Controllers;

[ApiController]
[Route("api/subcategories")]
public class SubCategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SubCategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubCategoryDto>>> GetAll([FromQuery] int? categoryId = null)
    {
        var query = _db.SubCategories
            .AsNoTracking()
            .Include(sc => sc.Category)
            .OrderBy(sc => sc.SortOrder)
            .ThenBy(sc => sc.DisplayName)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(sc => sc.CategoryId == categoryId);
        }

        var result = await query
            .Select(sc => new SubCategoryDto
            {
                Id = sc.Id,
                CategoryId = sc.CategoryId,
                CategoryName = sc.Category != null ? sc.Category.DisplayName : null,
                Name = sc.Name,
                DisplayName = sc.DisplayName,
                Description = sc.Description,
                SortOrder = sc.SortOrder,
                IsActive = sc.IsActive
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SubCategoryDto>> GetById(int id)
    {
        var subCategory = await _db.SubCategories
            .AsNoTracking()
            .Include(sc => sc.Category)
            .Where(sc => sc.Id == id)
            .Select(sc => new SubCategoryDto
            {
                Id = sc.Id,
                CategoryId = sc.CategoryId,
                CategoryName = sc.Category != null ? sc.Category.DisplayName : null,
                Name = sc.Name,
                DisplayName = sc.DisplayName,
                Description = sc.Description,
                SortOrder = sc.SortOrder,
                IsActive = sc.IsActive
            })
            .FirstOrDefaultAsync();

        if (subCategory is null)
        {
            return NotFound();
        }

        return Ok(subCategory);
    }

    [HttpPost]
    public async Task<ActionResult<SubCategoryDto>> Create(SubCategoryCreateDto dto)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists)
        {
            return BadRequest($"Category with id {dto.CategoryId} does not exist.");
        }

        var subCategory = new SubCategory
        {
            CategoryId = dto.CategoryId,
            Name = dto.Name,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive
        };

        _db.SubCategories.Add(subCategory);
        await _db.SaveChangesAsync();

        var created = await _db.SubCategories
            .AsNoTracking()
            .Include(sc => sc.Category)
            .Where(sc => sc.Id == subCategory.Id)
            .Select(sc => new SubCategoryDto
            {
                Id = sc.Id,
                CategoryId = sc.CategoryId,
                CategoryName = sc.Category != null ? sc.Category.DisplayName : null,
                Name = sc.Name,
                DisplayName = sc.DisplayName,
                Description = sc.Description,
                SortOrder = sc.SortOrder,
                IsActive = sc.IsActive
            })
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { id = subCategory.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SubCategoryDto>> Update(int id, SubCategoryUpdateDto dto)
    {
        var subCategory = await _db.SubCategories.FindAsync(id);
        if (subCategory is null)
        {
            return NotFound();
        }

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists)
        {
            return BadRequest($"Category with id {dto.CategoryId} does not exist.");
        }

        subCategory.CategoryId = dto.CategoryId;
        subCategory.Name = dto.Name;
        subCategory.DisplayName = dto.DisplayName;
        subCategory.Description = dto.Description;
        subCategory.SortOrder = dto.SortOrder;
        subCategory.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();

        var updated = await _db.SubCategories
            .AsNoTracking()
            .Include(sc => sc.Category)
            .Where(sc => sc.Id == subCategory.Id)
            .Select(sc => new SubCategoryDto
            {
                Id = sc.Id,
                CategoryId = sc.CategoryId,
                CategoryName = sc.Category != null ? sc.Category.DisplayName : null,
                Name = sc.Name,
                DisplayName = sc.DisplayName,
                Description = sc.Description,
                SortOrder = sc.SortOrder,
                IsActive = sc.IsActive
            })
            .FirstAsync();

        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var subCategory = await _db.SubCategories.FindAsync(id);
        if (subCategory is null)
        {
            return NotFound();
        }

        _db.SubCategories.Remove(subCategory);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
