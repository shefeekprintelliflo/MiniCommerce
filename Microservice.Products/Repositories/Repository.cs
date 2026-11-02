using System.Linq.Expressions;
using Microservice.Products.Data;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Products.Repositories;

// Shared EF Core code for every entity. Entity-specific repositories inherit it and add their own queries.
public class Repository<T>(ProductsDb db) : IRepository<T> where T : class
{
    protected DbSet<T> Set => db.Set<T>();

    // Tracked, so the result can be changed and then saved with UpdateAsync.
    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return Set.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    // Read-only list, so it is not tracked.
    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await Set.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
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

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        Set.Update(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}
