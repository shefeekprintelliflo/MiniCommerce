using Microservice.identity.Models;

namespace Microservice.identity.Repositories;

public interface IUserRepository : IRepository<AppUser>
{
    Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
