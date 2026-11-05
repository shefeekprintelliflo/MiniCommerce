using System.Security.Claims;
using Microservice.Orders.Data;
using Microservice.Orders.Models;
using Microservice.Orders.Payments;
using Microservice.Orders.Sagas;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Orders.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        // Every route is limited to the signed-in user's own orders.
        var group = app.MapGroup("/api/orders")
            .RequireAuthorization();

        group.MapPost("/", async Task<IResult> (
            HttpContext http, CheckoutRequest? request, CheckoutSaga saga, IPaymentStrategyFactory payments,
            ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var payment = payments.Create(request?.PaymentMethod ?? string.Empty);
            if (payment is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["paymentMethod"] = ["Choose a supported payment method."]
                });
            }

            var authorization = http.Request.Headers.Authorization.ToString();
            var email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var result = await saga.PlaceOrderAsync(CurrentUser.Id(user), email, authorization, payment, cancellationToken);

            if (result.Succeeded)
            {
                return Results.Created($"/api/orders/{result.Order!.Id}", result.Order);
            }

            return Results.Conflict(new { message = result.Error });
        });

        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, ClaimsPrincipal user, OrdersDb db, CancellationToken cancellationToken) =>
        {
            var userId = CurrentUser.Id(user);
            var order = await db.Orders.AsNoTracking()
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId, cancellationToken);

            return order is null ? Results.NotFound() : Results.Ok(OrderResponse.From(order));
        });

        group.MapGet("/", async (ClaimsPrincipal user, OrdersDb db, CancellationToken cancellationToken) =>
        {
            var userId = CurrentUser.Id(user);
            var orders = await db.Orders.AsNoTracking()
                .Include(o => o.Lines)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            return Results.Ok(orders.Select(OrderResponse.From));
        });

        return app;
    }
}

public static class CurrentUser
{
    // The identity service puts the user id in the "sub" claim, which ASP.NET maps to NameIdentifier.
    public static Guid Id(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Token has no subject claim."));
}
