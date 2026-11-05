namespace Microservice.Orders.Payments;

// Nothing is taken at checkout. The shopper pays on delivery, so there is no charge to refund.
public class CashOnDeliveryPaymentStrategy : IPaymentStrategy
{
    public string Method => "cashOnDelivery";

    public Task<PaymentResult> ChargeAsync(Guid orderId, decimal amount, CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentResult(true, $"cod_{orderId:N}", null));

    public Task RefundAsync(string reference, CancellationToken cancellationToken) => Task.CompletedTask;
}
