using Microservice.Products.Models;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Products.Data;

public class ProductsDb(DbContextOptions<ProductsDb> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<ReservationLine> ReservationLines => Set<ReservationLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(2000);
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.HasIndex(p => p.IsActive);
        });

        builder.Entity<Reservation>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasMany(r => r.Lines)
                .WithOne(l => l.Reservation)
                .HasForeignKey(l => l.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ReservationLine>(entity =>
        {
            entity.HasKey(l => new { l.ReservationId, l.ProductId });
            entity.Property(l => l.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(l => l.UnitPrice).HasPrecision(18, 2);
        });
    }
}
