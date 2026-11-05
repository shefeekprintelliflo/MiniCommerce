using System.Net;
using System.Net.Http.Json;

namespace Microservice.Orders.Clients;

public record ReservationRequestItem(Guid ProductId, int Quantity);

public record ReservedLine(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record ReservationResult(Guid ReservationId, IReadOnlyList<ReservedLine> Lines);

// Thrown when Products says the stock is not there. Any other failure is an HttpRequestException.
public class StockUnavailableException(string message) : Exception(message);

// Calls Microservice.Products. The shopper's bearer token is forwarded, so Products checks the same user.
public class ProductsClient(HttpClient http)
{
    public async Task<ReservationResult> ReserveAsync(
        Guid reservationId, IReadOnlyList<ReservationRequestItem> items, string authorization, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/products/reservations")
        {
            Content = JsonContent.Create(new { reservationId, items })
        };
        request.Headers.TryAddWithoutValidation("Authorization", authorization);

        using var response = await http.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var body = await response.Content.ReadFromJsonAsync<ConflictBody>(cancellationToken);
            throw new StockUnavailableException(body?.Message ?? "Some items are no longer available.");
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReservationResult>(cancellationToken))
            ?? throw new InvalidOperationException("Products returned an empty reservation.");
    }

    // Safe to call more than once. Products ignores a release it has already applied.
    public async Task ReleaseAsync(Guid reservationId, string authorization, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/products/reservations/{reservationId}/release");
        request.Headers.TryAddWithoutValidation("Authorization", authorization);

        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    private sealed record ConflictBody(string Message);
}
