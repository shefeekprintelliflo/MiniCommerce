namespace Microservice.Products.Models;

public record CreateProductRequest(string Name, string? Description, decimal Price, int StockQuantity, bool IsActive);

public record UpdateProductRequest(string Name, string? Description, decimal Price, int StockQuantity, bool IsActive);

public record ProductResponse(Guid Id, string Name, string? Description, decimal Price, int StockQuantity, bool IsActive, DateTime UpdatedAtUtc)
{
    public static ProductResponse From(Product product) =>
        new(product.Id, product.Name, product.Description, product.Price, product.StockQuantity, product.IsActive, product.UpdatedAtUtc);
}

public static class ProductValidation
{
    // Returns field errors in ValidationProblem format. An empty dictionary means the request is valid.
    public static Dictionary<string, string[]> Validate(string? name, string? description, decimal price, int stockQuantity)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["Name is required."];
        }
        else if (name.Length > 200)
        {
            errors["name"] = ["Name must be 200 characters or fewer."];
        }

        if (description?.Length > 2000)
        {
            errors["description"] = ["Description must be 2000 characters or fewer."];
        }

        if (price < 0)
        {
            errors["price"] = ["Price cannot be negative."];
        }

        if (stockQuantity < 0)
        {
            errors["stockQuantity"] = ["Stock quantity cannot be negative."];
        }

        return errors;
    }
}
