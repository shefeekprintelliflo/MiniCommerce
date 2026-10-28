using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Microservice.identity.Services;

// Applies the same password rules UserManager used to enforce, read from IdentityOptions.
public class PasswordPolicy(IOptions<IdentityOptions> options)
{
    private readonly PasswordOptions _rules = options.Value.Password;

    public List<IdentityError> Validate(string password)
    {
        var errors = new List<IdentityError>();

        if (password.Length < _rules.RequiredLength)
        {
            errors.Add(new IdentityError
            {
                Code = "PasswordTooShort",
                Description = $"Passwords must be at least {_rules.RequiredLength} characters."
            });
        }
        if (_rules.RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add(new IdentityError { Code = "PasswordRequiresDigit", Description = "Passwords must have at least one digit ('0'-'9')." });
        }
        if (_rules.RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add(new IdentityError { Code = "PasswordRequiresLower", Description = "Passwords must have at least one lowercase ('a'-'z')." });
        }
        if (_rules.RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add(new IdentityError { Code = "PasswordRequiresUpper", Description = "Passwords must have at least one uppercase ('A'-'Z')." });
        }
        if (_rules.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
        {
            errors.Add(new IdentityError { Code = "PasswordRequiresNonAlphanumeric", Description = "Passwords must have at least one non alphanumeric character." });
        }

        return errors;
    }
}
