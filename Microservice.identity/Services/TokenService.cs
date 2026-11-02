using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microservice.identity.Models;
using Microservice.identity.Options;
using Microservice.identity.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microservice.identity.Services;

// Issues short-lived access tokens and the refresh tokens that renew them.
public class TokenService(IOptions<JwtOptions> jwtOptions, IRefreshTokenRepository refreshTokens)
{
    private static readonly TimeSpan AccessLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);
    private const string RoleClaimType = "role";

    // Creates a new access token and a new refresh token. Only the refresh token's hash is saved.
    public async Task<AuthResponse> IssueAsync(AppUser user, IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        var accessExpires = DateTime.UtcNow.Add(AccessLifetime);
        var accessToken = CreateAccessToken(user, roles, accessExpires);

        var rawRefreshToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var refreshExpires = DateTime.UtcNow.Add(RefreshLifetime);

        await refreshTokens.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashRefreshToken(rawRefreshToken),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = refreshExpires
        }, cancellationToken);

        return new AuthResponse(accessToken, accessExpires, rawRefreshToken, refreshExpires);
    }

    // Lookups compare hashes, so the raw token is never stored and a database leak does not expose live tokens.
    public static string HashRefreshToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private string CreateAccessToken(AppUser user, IEnumerable<string> roles, DateTime expires)
    {
        var jwt = jwtOptions.Value;

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

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
