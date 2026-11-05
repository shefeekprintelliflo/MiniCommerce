using Microservice.Orders.Clients;

namespace Microservice.Orders.Events;

// Empties the shopper's cart once their order is placed.
public class ClearCartObserver(CartClient cart) : IOrderPlacedObserver
{
    public Task HandleAsync(OrderPlaced orderPlaced, CancellationToken cancellationToken) =>
        cart.ClearAsync(orderPlaced.Authorization, cancellationToken);
}
