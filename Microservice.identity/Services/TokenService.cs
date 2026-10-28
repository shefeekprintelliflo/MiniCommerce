using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microservice.identity.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Microservice.identity.Services;

// Issues short-lived access tokens. Refresh tokens are a later step.
public class TokenService(IConfiguration configuration)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    public AuthResponse CreateAccessToken(AppUser user)
    {
        var jwt = configuration.GetSection("Jwt");
        var expires = DateTime.UtcNow.Add(Lifetime);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!));
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
