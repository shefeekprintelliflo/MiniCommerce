using Microservice.identity.Data;
using Microservice.identity.Models;
using Microsoft.AspNetCore.Identity;

namespace Microservice.identity.Repositories;

public class UserRepository(IdentityDb db, ILookupNormalizer normalizer)
    : Repository<AppUser>(db), IUserRepository
{
    public Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = normalizer.NormalizeEmail(email);
        return FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = normalizer.NormalizeEmail(email);
        return AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public override Task AddAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        // Fill in the fields UserManager used to set, so lookups and token validation keep working.
        user.NormalizedUserName = normalizer.NormalizeName(user.UserName);
        user.NormalizedEmail = normalizer.NormalizeEmail(user.Email);
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.ConcurrencyStamp = Guid.NewGuid().ToString();

        return base.AddAsync(user, cancellationToken);
    }
}
