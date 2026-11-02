using Microservice.Products.Models;
using Microservice.Products.Repositories;

namespace Microservice.Products.Endpoints;

public static class AdminProductEndpoints
{
    public static IEndpointRouteBuilder MapAdminProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/products")
            .RequireAuthorization(AuthPolicies.AdminOnly);

        group.MapPost("/", async Task<IResult> (CreateProductRequest request, IProductRepository products) =>
        {
            var errors = ProductValidation.Validate(request.Name, request.Description, request.Price, request.StockQuantity);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var now = DateTime.UtcNow;
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                IsActive = request.IsActive,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await products.AddAsync(product);
            return Results.Created($"/api/products/{product.Id}", ProductResponse.From(product));
        });

        group.MapPut("/{id:guid}", async Task<IResult> (Guid id, UpdateProductRequest request, IProductRepository products) =>
        {
            var errors = ProductValidation.Validate(request.Name, request.Description, request.Price, request.StockQuantity);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var product = await products.FirstOrDefaultAsync(p => p.Id == id);
            if (product is null)
            {
                return Results.NotFound();
            }

            product.Name = request.Name.Trim();
            product.Description = request.Description?.Trim();
            product.Price = request.Price;
            product.StockQuantity = request.StockQuantity;
            product.IsActive = request.IsActive;
            product.UpdatedAtUtc = DateTime.UtcNow;

            await products.UpdateAsync(product);
            return Results.Ok(ProductResponse.From(product));
        });

        return app;
    }
}
