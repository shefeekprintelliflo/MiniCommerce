namespace Microservice.Notifications.Contracts;

// Must match Microservice.Orders.Events.OrderPlacedMessage. Both services serialize it as JSON with the same property names.
public record OrderPlacedMessage(Guid OrderId, Guid UserId, string Email, decimal Total, IReadOnlyList<PlacedLine> Lines);

public record PlacedLine(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, int RemainingStock);
