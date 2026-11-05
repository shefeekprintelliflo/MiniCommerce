namespace Microservice.Orders.Payments;

// Simulated PayPal payment. Replace the bodies with the PayPal SDK calls when one is chosen.
public class PayPalPaymentStrategy : IPaymentStrategy
{
    public string Method => "paypal";

    public Task<PaymentResult> ChargeAsync(Guid orderId, decimal amount, CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentResult(true, $"paypal_{orderId:N}", null));

    public Task RefundAsync(string reference, CancellationToken cancellationToken) => Task.CompletedTask;
}
