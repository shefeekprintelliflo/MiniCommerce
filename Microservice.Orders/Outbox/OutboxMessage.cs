using System.Text.Json;

namespace Microservice.Orders.Outbox;

// A message waiting to be published. It is saved in the same transaction as the order it describes,
// so an order can never be placed without its message, and a message never exists for an order that was not placed.
public class OutboxMessage
{
    public const string OrderPlacedType = "order.placed";

    public Guid Id { get; set; }

    // Sent as the message Subject, so consumers can filter by it.
    public string Type { get; set; } = string.Empty;

    // The order (or other entity) this message is about. Used as the Service Bus session, so messages for one order stay in order.
    public string AggregateId { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    // Null until Service Bus has accepted the message.
    public DateTime? PublishedAtUtc { get; set; }

    public int Attempts { get; set; }
    public string? LastError { get; set; }

    public static OutboxMessage For<T>(string type, string aggregateId, T payload) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        AggregateId = aggregateId,
        Payload = JsonSerializer.Serialize(payload),
        CreatedAtUtc = DateTime.UtcNow
    };
}
