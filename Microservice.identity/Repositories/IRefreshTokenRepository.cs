using Microservice.identity.Models;

namespace Microservice.identity.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    // Returns true only for the caller that actually flipped the token from active to revoked.
    Task<bool> TryRevokeAsync(Guid id, CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
