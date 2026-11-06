using Azure.Messaging.ServiceBus;
using Microservice.Orders.Data;
using Microservice.Orders.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Orders.Messaging;

// Background worker. Reads unpublished outbox rows and sends them to the Service Bus topic in Messaging:TopicName.
// Delivery is at-least-once: a message can be sent twice if the service stops between sending and saving.
// Consumers must therefore handle a repeated message safely.
public class ServiceBusOutboxPublisher(
    IServiceScopeFactory scopes,
    ServiceBusClient client,
    IConfiguration configuration,
    ILogger<ServiceBusOutboxPublisher> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxAttempts = 3;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private ServiceBusSender? sender;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topicName = configuration["Messaging:TopicName"];
        if (string.IsNullOrWhiteSpace(topicName))
        {
            // Without a topic, messages stay in the outbox until one is configured. Checkout still works.
            logger.LogWarning("Messaging:TopicName is not set. Outbox messages will not be published.");
            return;
        }

        sender = client.CreateSender(topicName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox publish pass failed. Retrying shortly.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDb>();

        // Messages that have failed MaxAttempts times are dead-lettered: they stay in the table with LastError
        // for someone to inspect, and are never picked up again.
        var pending = await db.OutboxMessages
            .Where(m => m.PublishedAtUtc == null && m.Attempts < MaxAttempts)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                await sender!.SendMessageAsync(new ServiceBusMessage(message.Payload)
                {
                    // Duplicate detection on the topic drops a resend with the same MessageId within its window.
                    MessageId = message.Id.ToString(),

                    // Sessions keep messages for one order in order. The subscription must have sessions enabled.
                    SessionId = message.AggregateId,

                    Subject = message.Type,
                    ContentType = "application/json"
                }, cancellationToken);

                message.PublishedAtUtc = DateTime.UtcNow;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Left unpublished, so the next pass retries it until MaxAttempts. The error is kept for diagnosis.
                message.Attempts++;
                message.LastError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;

                if (message.Attempts >= MaxAttempts)
                {
                    logger.LogError(ex, "Outbox message {MessageId} failed {Attempts} times and will not be retried.", message.Id, message.Attempts);
                }
                else
                {
                    logger.LogWarning(ex, "Could not publish outbox message {MessageId} (attempt {Attempts}).", message.Id, message.Attempts);
                }
            }

            // Saved one at a time, so a crash after some sends does not resend the ones already accepted.
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (sender is not null)
        {
            await sender.DisposeAsync();
        }
    }
}
