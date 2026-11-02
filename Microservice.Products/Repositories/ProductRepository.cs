using Microservice.Products.Data;
using Microservice.Products.Models;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Products.Repositories;

public class ProductRepository(ProductsDb db) : Repository<Product>(db), IProductRepository
{
    // Customer-facing list: only active products, sorted by name.
    public async Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await Set.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
}
