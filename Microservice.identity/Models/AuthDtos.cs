namespace Microservice.identity.Models;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);

// Used by both /refresh and /logout.
public record RefreshRequest(string RefreshToken);

public record RoleAssignmentRequest(string Role);
