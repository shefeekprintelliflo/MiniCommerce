namespace Microservice.Orders.Events;

// The subject. Holds every registered observer and calls each one when an order is placed.
// One observer failing does not stop the others, and does not undo the order: the order is already saved.
public class OrderEvents(IEnumerable<IOrderPlacedObserver> observers, ILogger<OrderEvents> logger)
{
    public async Task PublishOrderPlacedAsync(OrderPlaced orderPlaced, CancellationToken cancellationToken)
    {
        foreach (var observer in observers)
        {
            try
            {
                await observer.HandleAsync(orderPlaced, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "{Observer} failed for order {OrderId}.", observer.GetType().Name, orderPlaced.OrderId);
            }
        }
    }
}
