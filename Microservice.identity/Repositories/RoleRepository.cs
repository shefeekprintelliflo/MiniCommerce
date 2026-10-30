using Microservice.identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Microservice.identity.Repositories;

public class RoleRepository(IdentityDb db, ILookupNormalizer normalizer)
    : Repository<IdentityRole<Guid>>(db), IRoleRepository
{
    public Task<IdentityRole<Guid>?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = normalizer.NormalizeName(name);
        return FirstOrDefaultAsync(r => r.NormalizedName == normalizedName, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetUserRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await db.Set<IdentityUserRole<Guid>>()
            .Where(ur => ur.UserId == userId)
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
            .ToListAsync(cancellationToken);
    }

    public async Task AddToRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var links = db.Set<IdentityUserRole<Guid>>();
        if (await links.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken))
        {
            return;
        }

        links.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = roleId });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFromRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var links = db.Set<IdentityUserRole<Guid>>();
        var link = await links.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        if (link is null)
        {
            return;
        }

        links.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
    }

    public override Task AddAsync(IdentityRole<Guid> role, CancellationToken cancellationToken = default)
    {
        // Fill in the fields RoleManager used to set, so name lookups and the unique index keep working.
        if (role.Id == Guid.Empty)
        {
            role.Id = Guid.NewGuid();
        }
        role.NormalizedName = normalizer.NormalizeName(role.Name);
        role.ConcurrencyStamp = Guid.NewGuid().ToString();

        return base.AddAsync(role, cancellationToken);
    }
}
