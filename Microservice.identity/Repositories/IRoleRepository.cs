using Microsoft.AspNetCore.Identity;

namespace Microservice.identity.Repositories;

public interface IRoleRepository : IRepository<IdentityRole<Guid>>
{
    Task<IdentityRole<Guid>?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetUserRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddToRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task RemoveFromRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}
