using System.Text;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microservice.Orders.Clients;
using Microservice.Orders.Data;
using Microservice.Orders.Endpoints;
using Microservice.Orders.Events;
using Microservice.Orders.Messaging;
using Microservice.Orders.Notifications;
using Microservice.Orders.Options;
using Microservice.Orders.Payments;
using Microservice.Orders.Sagas;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrdersDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrdersDb")
        ?? throw new InvalidOperationException("Missing connection string 'OrdersDb'.")));

// Service addresses come from the Services section. Cart and Products are called through these clients.
var cartBaseUrl = builder.Configuration["Services:Cart"]
    ?? throw new InvalidOperationException("Missing setting 'Services:Cart'.");
var productsBaseUrl = builder.Configuration["Services:Products"]
    ?? throw new InvalidOperationException("Missing setting 'Services:Products'.");

builder.Services.AddHttpClient<CartClient>(client => client.BaseAddress = new Uri(cartBaseUrl));
builder.Services.AddHttpClient<ProductsClient>(client => client.BaseAddress = new Uri(productsBaseUrl));
builder.Services.AddScoped<CheckoutSaga>();

// Observers of OrderPlaced. OrderEvents calls every registered IOrderPlacedObserver. Add a reaction by registering another one here.
builder.Services.AddScoped<OrderEvents>();
builder.Services.AddScoped<IOrderPlacedObserver, ClearCartObserver>();
builder.Services.AddScoped<IOrderPlacedObserver, ConfirmationEmailObserver>();
builder.Services.AddScoped<IOrderPlacedObserver, LowStockObserver>();
builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();

// Outbox messages are sent to Azure Service Bus by a background worker. It signs in with DefaultAzureCredential:
// your az login locally, or the app's managed identity in Azure. No connection string or key is stored in config.
var serviceBusNamespace = builder.Configuration["Messaging:Namespace"];
if (!string.IsNullOrWhiteSpace(serviceBusNamespace))
{
    builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusNamespace, new DefaultAzureCredential()));
    builder.Services.AddHostedService<ServiceBusOutboxPublisher>();
}

// Each payment method is a strategy. The factory picks one by name. Add a new method by registering another strategy here.
builder.Services.AddSingleton<IPaymentStrategy, CardPaymentStrategy>();
builder.Services.AddSingleton<IPaymentStrategy, PayPalPaymentStrategy>();
builder.Services.AddSingleton<IPaymentStrategy, CashOnDeliveryPaymentStrategy>();
builder.Services.AddSingleton<IPaymentStrategyFactory, PaymentStrategyFactory>();

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

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks().AddDbContextCheck<OrdersDb>("sql");

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapOrderEndpoints();
app.MapHealthChecks("/health/ready");
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));

// Creates the database and applies pending migrations. Turned on by docker-compose; off for local runs.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<OrdersDb>().Database.MigrateAsync();
}

app.Run();

public partial class Program { }
