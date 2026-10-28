using Microsoft.AspNetCore.Identity;

namespace Microservice.identity.Models;

// Extends the built-in IdentityUser so password hashing and lookups come for free.
public class AppUser : IdentityUser<Guid>
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
