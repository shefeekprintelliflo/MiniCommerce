using System.Net.Http.Json;

namespace Microservice.Orders.Clients;

public record CartLine(Guid ProductId, string Name, decimal UnitPrice, int Quantity);

public record CartSnapshot(IReadOnlyList<CartLine> Lines);

// Calls Microservice.Cart. The shopper's bearer token is forwarded, so Cart returns that user's cart.
public class CartClient(HttpClient http)
{
    public async Task<CartSnapshot> GetAsync(string authorization, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
        request.Headers.TryAddWithoutValidation("Authorization", authorization);

        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<CartSnapshot>(cancellationToken))
            ?? throw new InvalidOperationException("Cart returned an empty response.");
    }

    public async Task ClearAsync(string authorization, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/cart");
        request.Headers.TryAddWithoutValidation("Authorization", authorization);

        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
