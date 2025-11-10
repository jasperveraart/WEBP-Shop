using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PWebShop.Api.Dtos;
using PWebShop.Api.Services;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register-customer")]
    public async Task<ActionResult<AuthResultDto>> RegisterCustomer(RegisterCustomerRequestDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser is not null)
        {
            return Conflict(new AuthResultDto
            {
                Success = false,
                Message = "A user with this email already exists."
            });
        }

        var user = new ApplicationUser
        {
            Email = dto.Email,
            UserName = dto.Email,
            DisplayName = dto.DisplayName,
            DefaultShippingAddress = dto.DefaultShippingAddress,
            IsCustomer = true,
            IsPendingApproval = true,
            IsActive = false,
            IsBlocked = false
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new AuthResultDto
            {
                Success = false,
                Message = string.Join(" ", createResult.Errors.Select(e => e.Description))
            });
        }

        var roleResult = await _userManager.AddToRoleAsync(user, ApplicationRoleNames.Customer);
        if (!roleResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new AuthResultDto
            {
                Success = false,
                Message = string.Join(" ", roleResult.Errors.Select(e => e.Description))
            });
        }

        return Ok(new AuthResultDto
        {
            Success = true,
            Message = "Customer registration submitted. Awaiting approval."
        });
    }

    [HttpPost("register-supplier")]
    public async Task<ActionResult<AuthResultDto>> RegisterSupplier(RegisterSupplierRequestDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser is not null)
        {
            return Conflict(new AuthResultDto
            {
                Success = false,
                Message = "A user with this email already exists."
            });
        }

        var user = new ApplicationUser
        {
            Email = dto.Email,
            UserName = dto.Email,
            DisplayName = dto.DisplayName,
            CompanyName = dto.CompanyName,
            VatNumber = dto.VatNumber,
            IsSupplier = true,
            IsPendingApproval = true,
            IsActive = false,
            IsBlocked = false
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new AuthResultDto
            {
                Success = false,
                Message = string.Join(" ", createResult.Errors.Select(e => e.Description))
            });
        }

        var roleResult = await _userManager.AddToRoleAsync(user, ApplicationRoleNames.Supplier);
        if (!roleResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new AuthResultDto
            {
                Success = false,
                Message = string.Join(" ", roleResult.Errors.Select(e => e.Description))
            });
        }

        return Ok(new AuthResultDto
        {
            Success = true,
            Message = "Supplier registration submitted. Awaiting approval."
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            return Unauthorized("Invalid credentials.");
        }

        if (user.IsBlocked)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "User account is blocked.");
        }

        if (!user.IsActive)
        {
            return Unauthorized("User account is not active yet.");
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
        if (!signInResult.Succeeded)
        {
            return Unauthorized("Invalid credentials.");
        }

        var tokenResult = await _jwtTokenService.GenerateTokenAsync(user);

        return Ok(new LoginResponseDto
        {
            Token = tokenResult.Token,
            ExpiresAtUtc = tokenResult.ExpiresAtUtc,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName ?? string.Empty,
            Roles = tokenResult.Roles
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponseDto>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new CurrentUserResponseDto
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
}
