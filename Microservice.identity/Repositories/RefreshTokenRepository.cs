using Microservice.identity.Data;
using Microservice.identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Microservice.identity.Repositories;

public class RefreshTokenRepository(IdentityDb db) : Repository<RefreshToken>(db), IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);
    }

    // The WHERE clause runs in SQL Server, so two concurrent refreshes cannot both win.
    public async Task<bool> TryRevokeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rows = await Set
            .Where(r => r.Id == id && r.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAtUtc, DateTime.UtcNow), cancellationToken);

        return rows == 1;
    }

    public Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Set
            .Where(r => r.UserId == userId && r.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAtUtc, DateTime.UtcNow), cancellationToken);
    }
}
