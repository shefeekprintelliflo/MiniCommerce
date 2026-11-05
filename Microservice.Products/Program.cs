using System.Text;
using Microservice.Products.Data;
using Microservice.Products.Endpoints;
using Microservice.Products.Models;
using Microservice.Products.Options;
using Microservice.Products.Repositories;
using Microservice.Products.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ProductsDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProductsDb")
        ?? throw new InvalidOperationException("Missing connection string 'ProductsDb'.")));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<StockReservationService>();

// Binds Jwt:* and fails at startup if any required value is missing.
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Validates bearer tokens issued by Microservice.identity. Issuer, audience and signing key must match its settings.
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

builder.Services.AddHealthChecks().AddDbContextCheck<ProductsDb>("sql");

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapAdminProductEndpoints();
app.MapProductEndpoints();
app.MapReservationEndpoints();
app.MapHealthChecks("/health/ready");
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));

// Creates the database and applies pending migrations. Turned on by docker-compose; off for local runs.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ProductsDb>().Database.MigrateAsync();
}

app.Run();

public partial class Program { }
