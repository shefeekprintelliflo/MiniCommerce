using Microservice.Products.Models;
using Microservice.Products.Services;

namespace Microservice.Products.Endpoints;

public static class ReservationEndpoints
{
    public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder app)
    {
        // Called by Microservice.Orders during checkout, with the shopper's own token forwarded.
        var group = app.MapGroup("/api/products/reservations")
            .RequireAuthorization();

        group.MapPost("/", async Task<IResult> (CreateReservationRequest request, StockReservationService stock, CancellationToken cancellationToken) =>
        {
            var duplicateProducts = request.Items.Select(i => i.ProductId).Distinct().Count() != request.Items.Count;
            if (request.Items.Count == 0 || request.Items.Any(i => i.Quantity <= 0) || duplicateProducts)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["items"] = ["Items must not be empty, quantities must be positive, and each product may appear once."]
                });
            }

            var (reservation, error) = await stock.ReserveAsync(request, cancellationToken);
            return error is null
                ? Results.Ok(reservation)
                : Results.Conflict(new ReservationConflictResponse(error));
        });

        // Idempotent: releasing a reservation that is already released is a no-op that still returns 204.
        group.MapPost("/{id:guid}/release", async Task<IResult> (Guid id, StockReservationService stock, CancellationToken cancellationToken) =>
        {
            var outcome = await stock.ReleaseAsync(id, cancellationToken);
            return outcome == ReleaseOutcome.NotFound ? Results.NotFound() : Results.NoContent();
        });

        return app;
    }
}
