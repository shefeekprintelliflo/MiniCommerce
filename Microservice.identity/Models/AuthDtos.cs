namespace Microservice.identity.Models;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string AccessToken, DateTime ExpiresAtUtc);

public record RoleAssignmentRequest(string Role);
