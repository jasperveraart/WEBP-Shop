using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PWebShop.Api.Options;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Api.Services;

public interface IJwtTokenService
{
    Task<JwtTokenResult> GenerateTokenAsync(ApplicationUser user);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtOptions _options;

    public JwtTokenService(UserManager<ApplicationUser> userManager, IOptions<JwtOptions> options)
    {
        _userManager = userManager;
        _options = options.Value;
    }

    public async Task<JwtTokenResult> GenerateTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
            new("displayName", user.DisplayName ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: creds);

        var encodedToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtTokenResult
        {
            Token = encodedToken,
            ExpiresAtUtc = expires,
            Roles = roles.ToArray()
        };
    }
}

public sealed class JwtTokenResult
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
}
