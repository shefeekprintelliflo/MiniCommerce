using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microservice.identity.Models;
using Microservice.identity.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microservice.identity.Services;

// Issues short-lived access tokens. Refresh tokens are a later step.
public class TokenService(IOptions<JwtOptions> jwtOptions)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);
    private const string RoleClaimType = "role";

    public AuthResponse CreateAccessToken(AppUser user, IEnumerable<string> roles)
    {
        var jwt = jwtOptions.Value;
        var expires = DateTime.UtcNow.Add(Lifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // One "role" claim per role. The bearer middleware maps these to ClaimTypes.Role for policies and [Authorize(Roles)].
        claims.AddRange(roles.Select(role => new Claim(RoleClaimType, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
