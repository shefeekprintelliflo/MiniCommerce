namespace Microservice.Orders.Events;

// Raised once an order is saved as Placed. Observers react to it without the saga knowing what they do.
// Email is the shopper's address from their token, and is empty if the token has none.
// Authorization is the shopper's bearer token. Observers that call other services forward it, as the saga does.
public record OrderPlaced(Guid OrderId, Guid UserId, string Email, decimal Total, IReadOnlyList<PlacedLine> Lines, string Authorization);

// RemainingStock is what Products had left for this product after the order's hold.
public record PlacedLine(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, int RemainingStock);
