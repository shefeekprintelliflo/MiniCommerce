namespace Microservice.Products.Models;

// A stock hold for one checkout. The checkout saga uses the order id as the reservation id.
public class Reservation
{
    public Guid Id { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Held;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }
    public List<ReservationLine> Lines { get; set; } = [];
}

// Price and name are copied at reservation time, so the order uses the price the shopper was reserved at.
public class ReservationLine
{
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public enum ReservationStatus
{
    Held,
    Released
}
