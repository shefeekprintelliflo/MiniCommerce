using System.ComponentModel.DataAnnotations;

namespace Microservice.Cart.Options;

// Must match the Jwt section used by Microservice.identity, so tokens it issues validate here.
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
