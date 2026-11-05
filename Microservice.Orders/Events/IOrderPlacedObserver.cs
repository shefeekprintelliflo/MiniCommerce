namespace Microservice.Orders.Events;

// One reaction to OrderPlaced. Register each observer with DI; OrderEvents calls all of them.
public interface IOrderPlacedObserver
{
    Task HandleAsync(OrderPlaced orderPlaced, CancellationToken cancellationToken);
}
