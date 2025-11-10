using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWebShop.Api.Dtos;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = ApplicationRoleNames.Administrator)]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AdminUserSummaryDto>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _userManager.Users.AsNoTracking();
        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<AdminUserSummaryDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new AdminUserSummaryDto
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName ?? string.Empty,
                IsCustomer = user.IsCustomer,
                IsSupplier = user.IsSupplier,
                IsEmployee = user.IsEmployee,
                IsAdministrator = user.IsAdministrator,
                IsActive = user.IsActive,
                IsPendingApproval = user.IsPendingApproval,
                IsBlocked = user.IsBlocked,
                DefaultShippingAddress = user.DefaultShippingAddress,
                CompanyName = user.CompanyName,
                VatNumber = user.VatNumber,
                Roles = roles
            });
        }

        return Ok(new PagedResultDto<AdminUserSummaryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, UpdateUserStatusRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Action))
        {
            return BadRequest("Action is required.");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var normalizedAction = dto.Action.Trim().ToLowerInvariant();
        switch (normalizedAction)
        {
            case "activate":
                user.IsActive = true;
                user.IsPendingApproval = false;
                user.IsBlocked = false;
                break;
            case "deactivate":
                user.IsActive = false;
                break;
            case "block":
                user.IsBlocked = true;
                user.IsActive = false;
                break;
            case "unblock":
                user.IsBlocked = false;
                break;
            default:
                return BadRequest("Invalid action. Use activate, deactivate, block or unblock.");
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, string.Join(" ", updateResult.Errors.Select(e => e.Description)));
        }

        return NoContent();
    }

    [HttpPut("{id}/roles")]
    public async Task<IActionResult> UpdateRoles(string id, UpdateUserRolesRequestDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var requestedRoles = dto.Roles?
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        var canonicalRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in requestedRoles)
        {
            var matchedRole = ApplicationRoleNames.All.FirstOrDefault(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
            if (matchedRole is null)
            {
                return BadRequest($"Role '{role}' is not supported.");
            }

            canonicalRoles.Add(matchedRole);
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (user.Id == currentUserId && userRoles.Contains(ApplicationRoleNames.Administrator) &&
            !canonicalRoles.Contains(ApplicationRoleNames.Administrator))
        {
            return BadRequest("Administrators cannot remove their own Administrator role.");
        }

        var rolesToAdd = ApplicationRoleNames.All
            .Where(r => canonicalRoles.Contains(r) && !userRoles.Contains(r))
            .ToList();

        var rolesToRemove = userRoles
            .Where(r => !canonicalRoles.Contains(r))
            .ToList();

        if (rolesToAdd.Any())
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, string.Join(" ", addResult.Errors.Select(e => e.Description)));
            }
        }

        if (rolesToRemove.Any())
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, string.Join(" ", removeResult.Errors.Select(e => e.Description)));
            }
        }

        user.IsCustomer = canonicalRoles.Contains(ApplicationRoleNames.Customer);
        user.IsSupplier = canonicalRoles.Contains(ApplicationRoleNames.Supplier);
        user.IsEmployee = canonicalRoles.Contains(ApplicationRoleNames.Employee);
        user.IsAdministrator = canonicalRoles.Contains(ApplicationRoleNames.Administrator);

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, string.Join(" ", updateResult.Errors.Select(e => e.Description)));
        }

        return NoContent();
    }
}
