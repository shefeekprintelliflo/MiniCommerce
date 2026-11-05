using System.Security.Claims;
using Microservice.Cart.Data;
using Microservice.Cart.Models;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Cart.Endpoints;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        // Each signed-in user has one cart. The user id comes from the token, never from the request.
        var group = app.MapGroup("/api/cart")
            .RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user, CartDb db, CancellationToken cancellationToken) =>
        {
            var userId = CurrentUser.Id(user);
            var items = await db.Items.AsNoTracking()
                .Where(i => i.UserId == userId)
                .OrderBy(i => i.ProductName)
                .ToListAsync(cancellationToken);

            return Results.Ok(CartResponse.From(items));
        });

        group.MapPut("/items/{productId:guid}", async Task<IResult> (
            Guid productId, SetCartItemRequest request, ClaimsPrincipal user, CartDb db, CancellationToken cancellationToken) =>
        {
            if (request.Quantity < 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["quantity"] = ["Quantity cannot be negative."]
                });
            }

            var userId = CurrentUser.Id(user);
            var item = await db.Items.FirstOrDefaultAsync(
                i => i.UserId == userId && i.ProductId == productId, cancellationToken);

            if (request.Quantity == 0)
            {
                if (item is not null)
                {
                    db.Items.Remove(item);
                }
            }
            else if (item is null)
            {
                db.Items.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    ProductName = request.Name.Trim(),
                    UnitPrice = request.UnitPrice,
                    Quantity = request.Quantity,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                item.ProductName = request.Name.Trim();
                item.UnitPrice = request.UnitPrice;
                item.Quantity = request.Quantity;
                item.UpdatedAtUtc = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(await LoadCartAsync(userId, db, cancellationToken));
        });

        group.MapDelete("/", async (ClaimsPrincipal user, CartDb db, CancellationToken cancellationToken) =>
        {
            var userId = CurrentUser.Id(user);
            await db.Items.Where(i => i.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            return Results.Ok(await LoadCartAsync(userId, db, cancellationToken));
        });

        return app;
    }

    private static async Task<CartResponse> LoadCartAsync(Guid userId, CartDb db, CancellationToken cancellationToken)
    {
        var items = await db.Items.AsNoTracking()
            .Where(i => i.UserId == userId)
            .OrderBy(i => i.ProductName)
            .ToListAsync(cancellationToken);

        return CartResponse.From(items);
    }
}

public static class CurrentUser
{
    // The identity service puts the user id in the "sub" claim, which ASP.NET maps to NameIdentifier.
    public static Guid Id(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Token has no subject claim."));
}
