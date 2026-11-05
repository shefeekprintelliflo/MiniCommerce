using System.Text;
using Microservice.Orders.Notifications;

namespace Microservice.Orders.Events;

// Emails the shopper a receipt once their order is placed. Skipped when the token has no email address.
public class ConfirmationEmailObserver(IEmailSender email) : IOrderPlacedObserver
{
    public Task HandleAsync(OrderPlaced orderPlaced, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderPlaced.Email))
        {
            return Task.CompletedTask;
        }

        var body = new StringBuilder();
        body.AppendLine($"Thank you for your order {orderPlaced.OrderId}.");
        body.AppendLine();
        foreach (var line in orderPlaced.Lines)
        {
            body.AppendLine($"{line.ProductName} x {line.Quantity}: {line.UnitPrice * line.Quantity:0.00}");
        }
        body.AppendLine();
        body.AppendLine($"Total: {orderPlaced.Total:0.00}");

        return email.SendAsync(orderPlaced.Email, $"Order {orderPlaced.OrderId} confirmed", body.ToString(), cancellationToken);
    }
}
