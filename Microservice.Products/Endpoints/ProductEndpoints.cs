using Microservice.Products.Models;
using Microservice.Products.Repositories;

namespace Microservice.Products.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        // Any signed-in user with a valid token from Microservice.identity can browse. Inactive products are hidden.
        var group = app.MapGroup("/api/products")
            .RequireAuthorization();

        group.MapGet("/", async (IProductRepository products) =>
        {
            var active = await products.GetActiveAsync();
            return Results.Ok(active.Select(ProductResponse.From));
        });

        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, IProductRepository products) =>
        {
            var product = await products.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            return product is null ? Results.NotFound() : Results.Ok(ProductResponse.From(product));
        });

        return app;
    }
}
