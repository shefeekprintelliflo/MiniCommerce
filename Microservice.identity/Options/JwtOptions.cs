using System.ComponentModel.DataAnnotations;

namespace Microservice.identity.Options;

// Bound from the "Jwt" section of appsettings.json.
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    public string SigningKey { get; set; } = string.Empty;
}
