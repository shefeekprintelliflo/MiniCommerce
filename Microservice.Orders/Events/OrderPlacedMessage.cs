namespace Microservice.Orders.Events;

// The shape published to Service Bus for other services. Unlike OrderPlaced, it never carries the shopper's bearer token.
public record OrderPlacedMessage(Guid OrderId, Guid UserId, string Email, decimal Total, IReadOnlyList<PlacedLine> Lines);
