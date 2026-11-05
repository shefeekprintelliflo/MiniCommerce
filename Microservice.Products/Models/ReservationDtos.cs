namespace Microservice.Products.Models;

// Sent by Microservice.Orders during checkout. ReservationId is the order id.
public record CreateReservationRequest(Guid ReservationId, List<ReservationItem> Items);

public record ReservationItem(Guid ProductId, int Quantity);

public record ReservedLine(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record ReservationResponse(Guid ReservationId, IReadOnlyList<ReservedLine> Lines)
{
    public static ReservationResponse From(Reservation reservation) =>
        new(reservation.Id, reservation.Lines
            .Select(line => new ReservedLine(line.ProductId, line.ProductName, line.UnitPrice, line.Quantity))
            .ToList());
}

public record ReservationConflictResponse(string Message);
