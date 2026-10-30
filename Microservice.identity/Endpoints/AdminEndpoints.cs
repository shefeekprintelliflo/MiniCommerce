using System.Security.Claims;
using Microservice.identity.Models;
using Microservice.identity.Repositories;

namespace Microservice.identity.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users")
            .RequireAuthorization(AuthPolicies.AdminOnly);

        group.MapPost("/{userId:guid}/roles", async (
            Guid userId,
            RoleAssignmentRequest request,
            IUserRepository users,
            IRoleRepository roles) =>
        {
            var user = await users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                return Results.NotFound();
            }

            var role = await roles.FindByNameAsync(request.Role);
            if (role is null)
            {
                return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: $"Unknown role '{request.Role}'.");
            }

            await roles.AddToRoleAsync(user.Id, role.Id);
            return Results.NoContent();
        });

        group.MapDelete("/{userId:guid}/roles/{roleName}", async (
            Guid userId,
            string roleName,
            ClaimsPrincipal caller,
            IUserRepository users,
            IRoleRepository roles) =>
        {
            var user = await users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                return Results.NotFound();
            }

            var role = await roles.FindByNameAsync(roleName);
            if (role is null)
            {
                return Results.NotFound();
            }

            // Stops an admin from removing their own Admin role and locking themselves out.
            if (role.Name == AppRoles.Admin && caller.FindFirstValue(ClaimTypes.NameIdentifier) == userId.ToString())
            {
                return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "You cannot remove your own Admin role.");
            }

            await roles.RemoveFromRoleAsync(user.Id, role.Id);
            return Results.NoContent();
        });

        return app;
    }
}
