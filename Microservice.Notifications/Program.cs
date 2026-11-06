using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microservice.Notifications.Messaging;
using Microservice.Notifications.Sending;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Same settings as Microservice.Orders. Messaging:ConnectionString (user secret) wins; otherwise DefaultAzureCredential
// signs in with your az login locally or the managed identity in Azure.
var connectionString = builder.Configuration["Messaging:ConnectionString"];
var serviceBusNamespace = builder.Configuration["Messaging:Namespace"];
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddSingleton(_ => new ServiceBusClient(connectionString));
}
else if (!string.IsNullOrWhiteSpace(serviceBusNamespace))
{
    builder.Services.AddSingleton(_ => new ServiceBusClient(
        fullyQualifiedNamespace: serviceBusNamespace,
        credential: new DefaultAzureCredential()));
}
else
{
    throw new InvalidOperationException("Set Messaging:ConnectionString or Messaging:Namespace.");
}

// Placeholder sender that logs the email. Replace with a real provider when one is chosen.
builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();
builder.Services.AddHostedService<OrderPlacedConsumer>();

var host = builder.Build();
host.Run();
