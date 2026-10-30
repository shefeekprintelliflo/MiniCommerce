using Microservice.identity.Models;
using Microservice.identity.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Microservice.identity.Services;

// Runs at startup. Creates the app roles if missing and, when configured, grants Admin to the bootstrap account.
// Safe on every start because it only adds what is missing.
public class RoleSeeder(IRoleRepository roles, IUserRepository users, IConfiguration configuration, ILogger<RoleSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in new[] { AppRoles.Admin, AppRoles.Customer })
        {
            if (await roles.FindByNameAsync(roleName, cancellationToken) is null)
            {
                await roles.AddAsync(new IdentityRole<Guid> { Name = roleName }, cancellationToken);
            }
        }

        var bootstrapEmail = configuration["Roles:BootstrapAdminEmail"];
        if (string.IsNullOrWhiteSpace(bootstrapEmail))
        {
            return;
        }

        var admin = await users.FindByEmailAsync(bootstrapEmail, cancellationToken);
        if (admin is null)
        {
            logger.LogWarning("Bootstrap admin '{Email}' does not exist yet. Register it, then restart to grant Admin.", bootstrapEmail);
            return;
        }

        var adminRole = await roles.FindByNameAsync(AppRoles.Admin, cancellationToken);
        await roles.AddToRoleAsync(admin.Id, adminRole!.Id, cancellationToken);
    }
}
