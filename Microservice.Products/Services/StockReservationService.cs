using Microservice.Products.Data;
using Microservice.Products.Models;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Products.Services;

public enum ReleaseOutcome
{
    Released,
    AlreadyReleased,
    NotFound
}

// Holds and releases stock for a checkout. Each change runs in a transaction, so a failed hold leaves no partial stock change.
public class StockReservationService(ProductsDb db)
{
    public async Task<(ReservationResponse? Reservation, string? Error)> ReserveAsync(
        CreateReservationRequest request, CancellationToken cancellationToken = default)
    {
        // A retried checkout sends the same reservation id. Return the original hold instead of taking the stock twice.
        var existing = await db.Reservations.AsNoTracking()
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == request.ReservationId, cancellationToken);

        if (existing is not null)
        {
            return existing.Status == ReservationStatus.Held
                ? (ReservationResponse.From(existing), null)
                : (null, "This reservation has already been released.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var reservation = new Reservation
        {
            Id = request.ReservationId,
            Status = ReservationStatus.Held,
            CreatedAtUtc = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            var product = await db.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == item.ProductId && p.IsActive, cancellationToken);

            if (product is null)
            {
                return (null, "A product in your cart is no longer available.");
            }

            // The stock check and the decrement are one statement, so two checkouts cannot both take the last item.
            var updated = await db.Products
                .Where(p => p.Id == item.ProductId && p.StockQuantity >= item.Quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.StockQuantity, p => p.StockQuantity - item.Quantity)
                    .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);

            if (updated == 0)
            {
                return (null, $"Not enough stock for \"{product.Name}\".");
            }

            // Read inside the same transaction, after the decrement, so it is the stock this hold left behind.
            var remaining = await db.Products.AsNoTracking()
                .Where(p => p.Id == item.ProductId)
                .Select(p => p.StockQuantity)
                .FirstAsync(cancellationToken);

            reservation.Lines.Add(new ReservationLine
            {
                ReservationId = reservation.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = item.Quantity,
                RemainingStock = remaining
            });
        }

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (ReservationResponse.From(reservation), null);
    }

    public async Task<ReleaseOutcome> ReleaseAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Only one caller can change Held to Released. A retry matches no rows, so stock is not restored twice.
        var released = await db.Reservations
            .Where(r => r.Id == reservationId && r.Status == ReservationStatus.Held)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, ReservationStatus.Released)
                .SetProperty(r => r.ReleasedAtUtc, DateTime.UtcNow), cancellationToken);

        if (released == 0)
        {
            var exists = await db.Reservations.AnyAsync(r => r.Id == reservationId, cancellationToken);
            return exists ? ReleaseOutcome.AlreadyReleased : ReleaseOutcome.NotFound;
        }

        var lines = await db.ReservationLines.AsNoTracking()
            .Where(l => l.ReservationId == reservationId)
            .ToListAsync(cancellationToken);

        foreach (var line in lines)
        {
            await db.Products
                .Where(p => p.Id == line.ProductId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.StockQuantity, p => p.StockQuantity + line.Quantity)
                    .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return ReleaseOutcome.Released;
    }
}
