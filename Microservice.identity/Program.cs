using System.Text;
using Microservice.identity.Data;
using Microservice.identity.Endpoints;
using Microservice.identity.Models;
using Microservice.identity.Repositories;
using Microservice.identity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<PasswordPolicy>();

var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = jwt["SigningKey"] ?? throw new InvalidOperationException("Missing Jwt:SigningKey.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<TokenService>();
builder.Services.AddHealthChecks().AddDbContextCheck<IdentityDb>("sql");

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapHealthChecks("/health/ready");
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));

app.Run();

public partial class Program { }
