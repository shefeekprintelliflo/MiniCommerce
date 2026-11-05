namespace Microservice.Orders.Payments;

// Receives every registered IPaymentStrategy and returns the one whose Method matches the request.
public class PaymentStrategyFactory(IEnumerable<IPaymentStrategy> strategies) : IPaymentStrategyFactory
{
    private readonly Dictionary<string, IPaymentStrategy> byMethod =
        strategies.ToDictionary(strategy => strategy.Method, StringComparer.OrdinalIgnoreCase);

    public IPaymentStrategy? Create(string method) =>
        string.IsNullOrEmpty(method) ? null : byMethod.GetValueOrDefault(method);
}
