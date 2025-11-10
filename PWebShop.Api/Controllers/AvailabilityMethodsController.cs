using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Api.Dtos;
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

    [AllowAnonymous]
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

    [AllowAnonymous]
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

}
