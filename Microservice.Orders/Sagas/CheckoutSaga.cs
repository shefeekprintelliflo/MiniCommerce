using Microservice.Orders.Clients;
using Microservice.Orders.Data;
using Microservice.Orders.Models;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Orders.Sagas;

public record CheckoutResult(bool Succeeded, OrderResponse? Order, string? Error);

// Orchestrates checkout across services:
//   1. Read the cart from Microservice.Cart.
//   2. Reserve stock in Microservice.Products (the order id is the reservation id).
//   3. Save the order with the prices from the reservation.
//   4. Clear the cart.
// If step 2 or 3 fails, the reservation is released and the order is marked Cancelled.
// The order row is saved as Pending before step 2, so a crash leaves a visible trail.
public class CheckoutSaga(
    OrdersDb db,
    CartClient cart,
    ProductsClient products,
    ILogger<CheckoutSaga> logger)
{
    public async Task<CheckoutResult> PlaceOrderAsync(Guid userId, string authorization, CancellationToken cancellationToken)
    {
        var snapshot = await cart.GetAsync(authorization, cancellationToken);
        if (snapshot.Lines.Count == 0)
        {
            return new CheckoutResult(false, null, "Your cart is empty.");
        }

        var now = DateTime.UtcNow;
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, CreatedAtUtc = now, UpdatedAtUtc = now };
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        var items = snapshot.Lines
            .Select(line => new ReservationRequestItem(line.ProductId, line.Quantity))
            .ToList();

        ReservationResult reservation;
        try
        {
            reservation = await products.ReserveAsync(order.Id, items, authorization, cancellationToken);
        }
        catch (StockUnavailableException ex)
        {
            // Products rejected the hold, so there is nothing to release. Only the order needs cancelling.
            await CancelAsync(order, ex.Message, authorization, releaseStock: false, cancellationToken);
            return new CheckoutResult(false, OrderResponse.From(order), ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Products may have saved the hold before the call failed, so release it to be safe.
            logger.LogError(ex, "Stock reservation for order {OrderId} failed. Releasing.", order.Id);
            await CancelAsync(order, "Checkout could not reserve stock. Please try again.", authorization, releaseStock: true, cancellationToken);
            return new CheckoutResult(false, OrderResponse.From(order), "Checkout could not reserve stock. Please try again.");
        }

        try
        {
            order.Lines = reservation.Lines
                .Select(line => new OrderLine
                {
                    OrderId = order.Id,
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    UnitPrice = line.UnitPrice,
                    Quantity = line.Quantity
                })
                .ToList();
            order.Total = order.Lines.Sum(line => line.UnitPrice * line.Quantity);
            order.Status = OrderStatus.Placed;
            order.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Saving order {OrderId} failed after stock was reserved. Releasing.", order.Id);
            await CancelAsync(order, "Checkout could not save the order. Please try again.", authorization, releaseStock: true, cancellationToken);
            return new CheckoutResult(false, OrderResponse.From(order), "Checkout could not save the order. Please try again.");
        }

        // The order is placed. If clearing the cart fails, the shopper still has the order, so only log it.
        try
        {
            await cart.ClearAsync(authorization, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Order {OrderId} was placed, but the cart could not be cleared.", order.Id);
        }

        return new CheckoutResult(true, OrderResponse.From(order), null);
    }

    private async Task CancelAsync(
        Order order, string reason, string authorization, bool releaseStock, CancellationToken cancellationToken)
    {
        if (releaseStock)
        {
            try
            {
                await products.ReleaseAsync(order.Id, authorization, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The order is still cancelled. The stock hold stays until someone releases it by hand.
                logger.LogError(ex, "Could not release reservation {ReservationId}.", order.Id);
            }
        }

        order.Status = OrderStatus.Cancelled;
        order.FailureReason = reason;
        order.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(CancellationToken.None);
    }
}
