using Microservice.identity.Models;
using Microservice.identity.Repositories;
using Microservice.identity.Services;
using Microsoft.AspNetCore.Identity;

namespace Microservice.identity.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            IUserRepository users,
            IRoleRepository roles,
            IPasswordHasher<AppUser> hasher,
            PasswordPolicy passwordPolicy) =>
        {
            var errors = passwordPolicy.Validate(request.Password);

            if (await users.EmailExistsAsync(request.Email))
            {
                errors.Add(new IdentityError { Code = "DuplicateEmail", Description = $"Email '{request.Email}' is already taken." });
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(
                    errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
            }

            // Fetch the default role first so a missing role fails before the user row is written.
            var customerRole = await roles.FindByNameAsync(AppRoles.Customer)
                ?? throw new InvalidOperationException($"Role '{AppRoles.Customer}' has not been seeded.");

            var user = new AppUser { UserName = request.Email, Email = request.Email };
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            await users.AddAsync(user);
            await roles.AddToRoleAsync(user.Id, customerRole.Id);

            return Results.Created($"/api/users/{user.Id}", new { user.Id, user.Email });
        });

        group.MapPost("/login", async (
            LoginRequest request,
            IUserRepository users,
            IRoleRepository roles,
            IRepository<AuditLog> auditLogs,
            IPasswordHasher<AppUser> hasher,
            TokenService tokens) =>
        {
            var user = await users.FindByEmailAsync(request.Email);

            // Same response for unknown email and wrong password, so attackers cannot probe for accounts.
            if (user is null || user.PasswordHash is null ||
                hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            {
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password.");
            }

            // AuditLog.UserId is a required foreign key, so only successful logins can be recorded here.
            await auditLogs.AddAsync(new AuditLog
            {
                UserId = user.Id,
                Action = "Login",
                Details = $"Signed in as {user.Email}.",
                CreatedAt = DateTime.UtcNow
            });

            var roleNames = await roles.GetUserRoleNamesAsync(user.Id);
            return Results.Ok(tokens.CreateAccessToken(user, roleNames));
        });

        return app;
    }
}
