namespace Microservice.Orders.Notifications;

// Sends email for observers. No mail provider is chosen yet, so the default implementation only logs.
// Replace LoggingEmailSender with a real sender when one is picked.
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}
