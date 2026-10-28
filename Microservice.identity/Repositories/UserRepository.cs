using Microservice.identity.Data;
using Microservice.identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Microservice.identity.Repositories;

public class UserRepository(IdentityDb db, ILookupNormalizer normalizer) : IUserRepository
{
    public Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = normalizer.NormalizeEmail(email);
        return db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = normalizer.NormalizeEmail(email);
        return db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task AddAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        // Fill in the fields UserManager used to set, so lookups and token validation keep working.
        user.NormalizedUserName = normalizer.NormalizeName(user.UserName);
        user.NormalizedEmail = normalizer.NormalizeEmail(user.Email);
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.ConcurrencyStamp = Guid.NewGuid().ToString();

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }
}
