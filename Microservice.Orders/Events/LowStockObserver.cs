using Microservice.Orders.Notifications;

namespace Microservice.Orders.Events;

// Warns the admin when an order leaves a product at or below Stock:LowStockThreshold.
// Emails Notifications:AdminEmail when it is set. Otherwise it only logs a warning.
public class LowStockObserver(IEmailSender email, IConfiguration configuration, ILogger<LowStockObserver> logger) : IOrderPlacedObserver
{
    public async Task  HandleAsync(OrderPlaced orderPlaced, CancellationToken cancellationToken)
    {
        var threshold = configuration.GetValue("Stock:LowStockThreshold", 5);
        var low = orderPlaced.Lines.Where(line => line.RemainingStock <= threshold).ToList();
        if (low.Count == 0)
        {
            return;
        }

        var summary = string.Join(
            "\n", low.Select(line => $"{line.ProductName} ({line.ProductId}): {line.RemainingStock} left"));

        var admin = configuration["Notifications:AdminEmail"];
        if (string.IsNullOrWhiteSpace(admin))
        {
            logger.LogWarning("Low stock after order {OrderId}:\n{Summary}", orderPlaced.OrderId, summary);
            return;
        }

        await email.SendAsync(admin, $"Low stock after order {orderPlaced.OrderId}", summary, cancellationToken);
    }
}
