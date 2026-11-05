using Microservice.Cart.Models;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Cart.Data;

public class CartDb(DbContextOptions<CartDb> options) : DbContext(options)
{
    public DbSet<CartItem> Items => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // One row per user and product, so adding the same product again changes the quantity.
        builder.Entity<CartItem>(entity =>
        {
            entity.HasKey(i => new { i.UserId, i.ProductId });
            entity.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
        });
    }
}
