namespace Microservice.Products.Models;

// Role names issued by Microservice.identity in the "role" claim.
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
}

// Named authorization policies. Registered in Program.cs.
public static class AuthPolicies
{
    public const string AdminOnly = "AdminOnly";
}
