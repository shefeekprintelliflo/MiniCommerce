using System.Text;
using Azure.Identity;
using Microservice.identity.Data;
using Microservice.identity.Endpoints;
using Microservice.identity.Models;
using Microservice.identity.Options;
using Microservice.identity.Repositories;
using Microservice.identity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Loads secrets (such as ConnectionStrings--IdentityDb) from Azure Key Vault when KeyVault:Uri is set.
// Locally, DefaultAzureCredential uses your Azure CLI or Visual Studio login.
// In Azure, it uses the app's managed identity. Leave KeyVault:Uri empty to skip Key Vault.
var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

builder.Services.AddDbContext<IdentityDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityDb")
        ?? throw new InvalidOperationException("Missing connection string 'IdentityDb'.")));

// Identity pieces we still use: password rule settings, PBKDF2 hashing, and email/name normalization.
// AddIdentityCore is not called because it registers UserManager, which needs an IUserStore we no longer provide.
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
});
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddSingleton<ILookupNormalizer, UpperInvariantLookupNormalizer>();

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<RoleSeeder>();
builder.Services.AddSingleton<PasswordPolicy>();

// Binds Jwt:* to JwtOptions and fails at startup if any required value is missing.
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;

        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.AdminOnly, policy => policy.RequireRole(AppRoles.Admin));
});

// Scoped because it depends on the scoped refresh-token repository (and therefore on the DbContext).
builder.Services.AddScoped<TokenService>();
builder.Services.AddHealthChecks().AddDbContextCheck<IdentityDb>("sql");

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapAdminEndpoints();

app.MapHealthChecks("/health/ready");
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));

using (var scope = app.Services.CreateScope())
{
    // Creates the database and applies pending migrations. Turned on by docker-compose; off for local runs.
    if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    {
        await scope.ServiceProvider.GetRequiredService<IdentityDb>().Database.MigrateAsync();
    }

    await scope.ServiceProvider.GetRequiredService<RoleSeeder>().SeedAsync();
}

app.Run();

public partial class Program { }
