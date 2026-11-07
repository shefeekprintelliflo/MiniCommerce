using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

// Storage:ConnectionString (local/Azurite) wins. Otherwise DefaultAzureCredential uses your az login locally
// or the Function App's managed identity in Azure, with Storage:AccountUri set to https://<account>.blob.core.windows.net.
var connectionString = builder.Configuration["Storage:ConnectionString"];
var accountUri = builder.Configuration["Storage:AccountUri"];
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddSingleton(_ => new BlobServiceClient(connectionString));
}
else if (!string.IsNullOrWhiteSpace(accountUri))
{
    builder.Services.AddSingleton(_ => new BlobServiceClient(new Uri(accountUri), new DefaultAzureCredential()));
}
else
{
    throw new InvalidOperationException("Set Storage:ConnectionString or Storage:AccountUri.");
}

builder.Build().Run();
