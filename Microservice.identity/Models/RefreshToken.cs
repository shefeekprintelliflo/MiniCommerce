namespace Microservice.identity.Models;

// Only a SHA-256 hash of the token is stored. The raw value is returned to the client once and never persisted.
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    public AppUser User { get; set; } = null!;
}
