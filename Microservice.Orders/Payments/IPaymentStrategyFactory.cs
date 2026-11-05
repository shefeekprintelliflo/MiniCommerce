namespace Microservice.Orders.Payments;

public interface IPaymentStrategyFactory
{
    // Returns null when no strategy handles the method name.
    IPaymentStrategy? Create(string method);
}
