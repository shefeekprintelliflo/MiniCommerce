using System.Linq.Expressions;
using Microservice.identity.Data;
using Microsoft.EntityFrameworkCore;

namespace Microservice.identity.Repositories;

// Shared EF Core code for every entity. Entity-specific repositories inherit it and add their own queries.
public class Repository<T>(IdentityDb db) : IRepository<T> where T : class
{
    protected DbSet<T> Set => db.Set<T>();

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return Set.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return Set.AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        Set.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}
