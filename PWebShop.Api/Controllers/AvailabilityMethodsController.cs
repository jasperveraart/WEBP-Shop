using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Api.Dtos;
using PWebShop.Domain.Entities;
using PWebShop.Infrastructure;

namespace PWebShop.Api.Controllers;

[ApiController]
[Route("api/availabilitymethods")]
public class AvailabilityMethodsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AvailabilityMethodsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AvailabilityMethodDto>>> GetAll()
    {
        var methods = await _db.AvailabilityMethods
            .AsNoTracking()
            .OrderBy(am => am.DisplayName)
            .Select(am => new AvailabilityMethodDto
            {
                Id = am.Id,
                Name = am.Name,
                DisplayName = am.DisplayName,
                Description = am.Description,
                IsActive = am.IsActive
            })
            .ToListAsync();

        return Ok(methods);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AvailabilityMethodDto>> GetById(int id)
    {
        var method = await _db.AvailabilityMethods
            .AsNoTracking()
            .Where(am => am.Id == id)
            .Select(am => new AvailabilityMethodDto
            {
                Id = am.Id,
                Name = am.Name,
                DisplayName = am.DisplayName,
                Description = am.Description,
                IsActive = am.IsActive
            })
            .FirstOrDefaultAsync();

        if (method is null)
        {
            return NotFound();
        }

        return Ok(method);
    }

    [HttpPost]
    public async Task<ActionResult<AvailabilityMethodDto>> Create(AvailabilityMethodCreateDto dto)
    {
        var method = new AvailabilityMethod
        {
            Name = dto.Name,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            IsActive = dto.IsActive
        };

        _db.AvailabilityMethods.Add(method);
        await _db.SaveChangesAsync();

        var created = await _db.AvailabilityMethods
            .AsNoTracking()
            .Where(am => am.Id == method.Id)
            .Select(am => new AvailabilityMethodDto
            {
                Id = am.Id,
                Name = am.Name,
                DisplayName = am.DisplayName,
                Description = am.Description,
                IsActive = am.IsActive
            })
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { id = method.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AvailabilityMethodDto>> Update(int id, AvailabilityMethodUpdateDto dto)
    {
        var method = await _db.AvailabilityMethods.FindAsync(id);
        if (method is null)
        {
            return NotFound();
        }

        method.Name = dto.Name;
        method.DisplayName = dto.DisplayName;
        method.Description = dto.Description;
        method.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();

        var updated = await _db.AvailabilityMethods
            .AsNoTracking()
            .Where(am => am.Id == method.Id)
            .Select(am => new AvailabilityMethodDto
            {
                Id = am.Id,
                Name = am.Name,
                DisplayName = am.DisplayName,
                Description = am.Description,
                IsActive = am.IsActive
            })
            .FirstAsync();

        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var method = await _db.AvailabilityMethods.FindAsync(id);
        if (method is null)
        {
            return NotFound();
        }

        _db.AvailabilityMethods.Remove(method);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
