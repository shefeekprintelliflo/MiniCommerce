namespace Microservice.identity.Models;

// Role names stored in AspNetRoles.Name. Seeded at startup by RoleSeeder.
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
