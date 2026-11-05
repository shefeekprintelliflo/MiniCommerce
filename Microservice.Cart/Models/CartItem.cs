namespace Microservice.Cart.Models;

public class CartItem
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }

    // Name and price are a snapshot for display. Checkout takes the real price from Microservice.Products.
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
