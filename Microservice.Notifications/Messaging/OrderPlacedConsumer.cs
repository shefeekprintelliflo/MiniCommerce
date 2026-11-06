using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microservice.Notifications.Contracts;
using Microservice.Notifications.Sending;

namespace Microservice.Notifications.Messaging;

// Receives order.placed messages from the order-events queue and sends the shopper a confirmation email.
// Uses sessions, matching the publisher: messages for one order are handled in order.
// Delivery is at-least-once, so a redelivered message can send the same email twice.
// The message is completed only after the email call succeeds. If it fails, the message is abandoned and retried by Service Bus.
public class OrderPlacedConsumer(
    ServiceBusClient client,
    IConfiguration configuration,
    IEmailSender email,
    ILogger<OrderPlacedConsumer> logger) : BackgroundService
{
    private const string OrderPlacedSubject = "order.placed";
    private ServiceBusSessionProcessor? processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = configuration["Messaging:QueueName"];
        if (string.IsNullOrWhiteSpace(queueName))
        {
            logger.LogWarning("Messaging:QueueName is not set. Order confirmations will not be sent.");
            return;
        }

        processor = client.CreateSessionProcessor(queueName, new ServiceBusSessionProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentSessions = 4,
            MaxConcurrentCallsPerSession = 1
        });

        processor.ProcessMessageAsync += HandleMessageAsync;
        processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Service Bus error from {Source} on {Queue}.", args.ErrorSource, args.FullyQualifiedNamespace);
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(stoppingToken);
        logger.LogInformation("Listening for order messages on {Queue}.", queueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private async Task HandleMessageAsync(ProcessSessionMessageEventArgs args)
    {
        var message = args.Message;

        if (message.Subject != OrderPlacedSubject)
        {
            // Not for this service. Dead-letter it so it doesn't block the session or get retried forever.
            logger.LogWarning("Unexpected message type {Subject} (id {MessageId}). Dead-lettering.", message.Subject, message.MessageId);
            await args.DeadLetterMessageAsync(message, "UnexpectedType", $"Subject '{message.Subject}' is not handled here.");
            return;
        }

        OrderPlacedMessage? order;
        try
        {
            order = JsonSerializer.Deserialize<OrderPlacedMessage>(message.Body.ToString());
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Message {MessageId} is not valid JSON. Dead-lettering.", message.MessageId);
            await args.DeadLetterMessageAsync(message, "InvalidPayload", ex.Message);
            return;
        }

        if (order is null)
        {
            await args.DeadLetterMessageAsync(message, "EmptyPayload", "Payload deserialized to null.");
            return;
        }

        // Matches the in-process observer: no address means nothing to send.
        if (string.IsNullOrWhiteSpace(order.Email))
        {
            logger.LogInformation("Order {OrderId} has no email address. Skipping confirmation.", order.OrderId);
            await args.CompleteMessageAsync(message);
            return;
        }

        try
        {
            await email.SendAsync(order.Email, $"Order {order.OrderId} confirmed", BuildBody(order), args.CancellationToken);
            await args.CompleteMessageAsync(message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Abandon so Service Bus redelivers it. After the queue's max delivery count, it is dead-lettered by the broker.
            logger.LogWarning(ex, "Could not send confirmation for order {OrderId}. Will retry.", order.OrderId);
            await args.AbandonMessageAsync(message);
        }
    }

    private static string BuildBody(OrderPlacedMessage order)
    {
        var body = new StringBuilder();
        body.AppendLine($"Thank you for your order {order.OrderId}.");
        body.AppendLine();
        foreach (var line in order.Lines)
        {
            body.AppendLine($"{line.ProductName} x {line.Quantity}: {line.UnitPrice * line.Quantity:0.00}");
        }
        body.AppendLine();
        body.AppendLine($"Total: {order.Total:0.00}");
        return body.ToString();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (processor is not null)
        {
            await processor.StopProcessingAsync(cancellationToken);
            await processor.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}
