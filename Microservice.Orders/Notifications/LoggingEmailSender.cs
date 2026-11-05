namespace Microservice.Orders.Notifications;

// Placeholder sender. It writes the email to the log instead of delivering it.
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation("Email to {To}: {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
