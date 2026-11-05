using Microservice.Orders.Models;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Orders.Data;

public class OrdersDb(DbContextOptions<OrdersDb> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(o => o.Total).HasPrecision(18, 2);
            entity.Property(o => o.PaymentMethod).HasMaxLength(20);
            entity.Property(o => o.PaymentReference).HasMaxLength(100);
            entity.Property(o => o.FailureReason).HasMaxLength(500);
            entity.HasIndex(o => new { o.UserId, o.CreatedAtUtc });
            entity.HasMany(o => o.Lines)
                .WithOne(l => l.Order)
                .HasForeignKey(l => l.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderLine>(entity =>
        {
            entity.HasKey(l => new { l.OrderId, l.ProductId });
            entity.Property(l => l.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(l => l.UnitPrice).HasPrecision(18, 2);
        });
    }
}
