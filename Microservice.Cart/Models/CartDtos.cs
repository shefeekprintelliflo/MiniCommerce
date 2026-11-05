namespace Microservice.Cart.Models;

// Quantity 0 removes the product from the cart.
public record SetCartItemRequest(string Name, decimal UnitPrice, int Quantity);

public record CartLineResponse(Guid ProductId, string Name, decimal UnitPrice, int Quantity);

public record CartResponse(IReadOnlyList<CartLineResponse> Lines, int ItemCount, decimal Total)
{
    public static CartResponse From(IEnumerable<CartItem> items)
    {
        var lines = items
            .Select(item => new CartLineResponse(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity))
            .ToList();

        return new CartResponse(lines, lines.Sum(line => line.Quantity), lines.Sum(line => line.UnitPrice * line.Quantity));
    }
}
