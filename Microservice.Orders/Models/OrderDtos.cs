namespace Microservice.Orders.Models;

public record OrderLineResponse(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record OrderResponse(Guid Id, string Status, decimal Total, string? FailureReason, DateTime CreatedAtUtc, IReadOnlyList<OrderLineResponse> Lines)
{
    public static OrderResponse From(Order order) =>
        new(
            order.Id,
            order.Status.ToString(),
            order.Total,
            order.FailureReason,
            order.CreatedAtUtc,
            order.Lines.Select(l => new OrderLineResponse(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity)).ToList());
}
