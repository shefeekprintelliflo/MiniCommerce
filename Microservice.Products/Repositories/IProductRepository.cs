using Microservice.Products.Models;

namespace Microservice.Products.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken cancellationToken = default);
}
