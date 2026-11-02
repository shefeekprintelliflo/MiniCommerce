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
            return Results.Ok(await tokens.IssueAsync(user, roleNames));
        });

        // Rotates the refresh token: the presented one is revoked and a new pair is issued.
        group.MapPost("/refresh", async (
            RefreshRequest request,
            IUserRepository users,
            IRoleRepository roles,
            IRefreshTokenRepository refreshTokens,
            TokenService tokens) =>
        {
            var stored = await refreshTokens.FindByHashAsync(TokenService.HashRefreshToken(request.RefreshToken));
            if (stored is null || stored.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return InvalidRefresh();
            }

            // A revoked token being presented again means it was copied or stolen. Revoke every session for that user.
            if (stored.RevokedAtUtc is not null)
            {
                await refreshTokens.RevokeAllForUserAsync(stored.UserId);
                return InvalidRefresh();
            }

            // Only one concurrent caller can flip the token, so the losing caller is treated as reuse.
            if (!await refreshTokens.TryRevokeAsync(stored.Id))
            {
                await refreshTokens.RevokeAllForUserAsync(stored.UserId);
                return InvalidRefresh();
            }

            var user = await users.FirstOrDefaultAsync(u => u.Id == stored.UserId);
            if (user is null)
            {
                return InvalidRefresh();
            }

            var roleNames = await roles.GetUserRoleNamesAsync(user.Id);
            return Results.Ok(await tokens.IssueAsync(user, roleNames));
        });

        // Revokes the presented refresh token. Always returns 204 so the response does not reveal whether a token was valid.
        group.MapPost("/logout", async (
            RefreshRequest request,
            IRefreshTokenRepository refreshTokens) =>
        {
            var stored = await refreshTokens.FindByHashAsync(TokenService.HashRefreshToken(request.RefreshToken));
            if (stored is not null)
            {
                await refreshTokens.TryRevokeAsync(stored.Id);
            }

            return Results.NoContent();
        });

        return app;
    }

    // One generic 401 for every refresh failure, so callers cannot tell expired, unknown and reused tokens apart.
    private static IResult InvalidRefresh() =>
        Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid or expired refresh token.");
}
