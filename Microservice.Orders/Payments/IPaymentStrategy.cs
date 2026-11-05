namespace Microservice.Orders.Payments;

// Reference is what the gateway returns for a successful charge. It is kept on the order so the charge can be refunded.
public record PaymentResult(bool Succeeded, string? Reference, string? Error);

// One way of paying. Each payment method implements this, and PaymentStrategyFactory picks the right one by name.
public interface IPaymentStrategy
{
    // The name the browser sends, such as "card". Matched without regard to case.
    string Method { get; }

    Task<PaymentResult> ChargeAsync(Guid orderId, decimal amount, CancellationToken cancellationToken);

    // Undoes a charge. Pass the reference that ChargeAsync returned.
    Task RefundAsync(string reference, CancellationToken cancellationToken);
}
