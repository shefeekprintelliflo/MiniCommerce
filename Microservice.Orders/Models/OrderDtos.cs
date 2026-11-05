namespace Microservice.Orders.Models;

// Sent by the browser on checkout. PaymentMethod is the name of a registered payment strategy, such as "card".
public record CheckoutRequest(string? PaymentMethod);

public record OrderLineResponse(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record OrderResponse(Guid Id, string Status, decimal Total, string? PaymentMethod, string? FailureReason, DateTime CreatedAtUtc, IReadOnlyList<OrderLineResponse> Lines)
{
    public static OrderResponse From(Order order) =>
        new(
            order.Id,
            order.Status.ToString(),
            order.Total,
            order.PaymentMethod,
            order.FailureReason,
            order.CreatedAtUtc,
            order.Lines.Select(l => new OrderLineResponse(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity)).ToList());
}
