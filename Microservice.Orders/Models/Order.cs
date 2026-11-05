namespace Microservice.Orders.Models;

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Total { get; set; }
    public string? PaymentMethod { get; set; }
    // Returned by the payment strategy when the charge succeeds. Needed to refund it.
    public string? PaymentReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<OrderLine> Lines { get; set; } = [];
}

public class OrderLine
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public enum OrderStatus
{
    // Stock is held. The saga has not finished yet.
    Pending,
    Placed,
    Cancelled
}
