namespace Microservice.Orders.Payments;

// Simulated card payment. Replace the bodies with the card gateway's calls when one is chosen.
public class CardPaymentStrategy : IPaymentStrategy
{
    public string Method => "card";

    public Task<PaymentResult> ChargeAsync(Guid orderId, decimal amount, CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentResult(true, $"card_{orderId:N}", null));

    public Task RefundAsync(string reference, CancellationToken cancellationToken) => Task.CompletedTask;
}
