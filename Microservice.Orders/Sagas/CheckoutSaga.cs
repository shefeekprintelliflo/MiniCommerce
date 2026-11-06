using Microservice.Orders.Clients;
using Microservice.Orders.Data;
using Microservice.Orders.Events;
using Microservice.Orders.Models;
using Microservice.Orders.Outbox;
using Microservice.Orders.Payments;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Orders.Sagas;

public record CheckoutResult(bool Succeeded, OrderResponse? Order, string? Error);

// Orchestrates checkout across services:
//   1. Read the cart from Microservice.Cart and price it.
//   2. Save the order as Pending.
//   3. Charge the shopper with the chosen payment strategy.
//   4. Reserve stock in Microservice.Products (the order id is the reservation id).
//   5. Check the reserved prices match the amount charged, then save the order as Placed and its order.placed message in one save.
//   6. Publish OrderPlaced. Observers (such as clearing the cart) react to it.
// If a step after the charge fails, the charge is refunded, the reservation is released if one was made, and the order is marked Cancelled.
// The order row is saved as Pending before the charge, so a crash leaves a visible trail.
public class CheckoutSaga(
    OrdersDb db,
    CartClient cart,
    ProductsClient products,
    OrderEvents events,
    ILogger<CheckoutSaga> logger)
{
    public async Task<CheckoutResult> PlaceOrderAsync(
        Guid userId, string email, string authorization, IPaymentStrategy payment, CancellationToken cancellationToken)
    {
        var snapshot = await cart.GetAsync(authorization, cancellationToken);
        if (snapshot.Lines.Count == 0)
        {
            return new CheckoutResult(false, null, "Your cart is empty.");
        }

        // Charged before stock is reserved, so the amount comes from the cart. Step 5 checks Products agrees with it.
        var amount = snapshot.Lines.Sum(line => line.UnitPrice * line.Quantity);

        var now = DateTime.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PaymentMethod = payment.Method,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        PaymentResult charge;
        try
        {
            charge = await payment.ChargeAsync(order.Id, amount, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Payment for order {OrderId} threw. Nothing was reserved.", order.Id);
            await CancelAsync(order, "Payment could not be processed. Please try again.", cancellationToken);
            return new CheckoutResult(false, OrderResponse.From(order), "Payment could not be processed. Please try again.");
        }

        if (!charge.Succeeded)
        {
            // The gateway declined the charge, so there is nothing to refund and no stock to release.
            var reason = charge.Error ?? "Payment was declined.";
            await CancelAsync(order, reason, cancellationToken);
            return new CheckoutResult(false, OrderResponse.From(order), reason);
        }

        // Kept on the order before anything else can fail, so a cancelled order still shows which charge to refund.
        order.PaymentReference = charge.Reference;
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
            // Products rejected the hold, so there is nothing to release. The charge still has to be refunded.
            await RefundAsync(payment, order, cancellationToken);
            await CancelAsync(order, ex.Message, cancellationToken);
            return new CheckoutResult(false, OrderResponse.From(order), ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Products may have saved the hold before the call failed, so release it to be safe.
            logger.LogError(ex, "Stock reservation for order {OrderId} failed. Releasing.", order.Id);
            await RefundAsync(payment, order, cancellationToken);
            await CancelAsync(order, "Checkout could not reserve stock. Please try again.", cancellationToken, releaseStock: true, authorization);
            return new CheckoutResult(false, OrderResponse.From(order), "Checkout could not reserve stock. Please try again.");
        }

        var reservedTotal = reservation.Lines.Sum(line => line.UnitPrice * line.Quantity);
        if (reservedTotal != amount)
        {
            // Prices changed between the cart read and the reservation. The shopper was charged the old amount.
            logger.LogWarning("Order {OrderId} charged {Charged} but reserved at {Reserved}. Refunding.", order.Id, amount, reservedTotal);
            await RefundAsync(payment, order, cancellationToken);
            await CancelAsync(order, "Prices changed during checkout. Please review your cart and try again.", cancellationToken, releaseStock: true, authorization);
            return new CheckoutResult(false, OrderResponse.From(order), "Prices changed during checkout. Please review your cart and try again.");
        }

        var placedLines = reservation.Lines
            .Select(line => new PlacedLine(line.ProductId, line.ProductName, line.UnitPrice, line.Quantity, line.RemainingStock))
            .ToList();

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
            order.Total = reservedTotal;
            order.Status = OrderStatus.Placed;
            order.UpdatedAtUtc = DateTime.UtcNow;

            // Added to the same unit of work as the order, so both are saved together or not at all.
            db.OutboxMessages.Add(OutboxMessage.For(
                OutboxMessage.OrderPlacedType,
                order.Id.ToString(),
                new OrderPlacedMessage(order.Id, userId, email, reservedTotal, placedLines)));

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Saving order {OrderId} failed after stock was reserved. Releasing.", order.Id);
            await RefundAsync(payment, order, cancellationToken);
            await CancelAsync(order, "Checkout could not save the order. Please try again.", cancellationToken, releaseStock: true, authorization);
            return new CheckoutResult(false, OrderResponse.From(order), "Checkout could not save the order. Please try again.");
        }

        // The order is saved, so nothing below can cancel it. Observers run after this point and their failures are only logged.
        await events.PublishOrderPlacedAsync(
            new OrderPlaced(order.Id, userId, email, order.Total, placedLines, authorization), CancellationToken.None);

        return new CheckoutResult(true, OrderResponse.From(order), null);
    }

    // A refund that fails is logged, not thrown. The order is still cancelled and keeps its PaymentReference so someone can refund it by hand.
    private async Task RefundAsync(IPaymentStrategy payment, Order order, CancellationToken cancellationToken)
    {
        if (order.PaymentReference is null)
        {
            return;
        }

        try
        {
            await payment.RefundAsync(order.PaymentReference, CancellationToken.None);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not refund payment {Reference} for order {OrderId}.", order.PaymentReference, order.Id);
        }
    }

    private async Task CancelAsync(
        Order order, string reason, CancellationToken cancellationToken,
        bool releaseStock = false, string? authorization = null)
    {
        if (releaseStock && authorization is not null)
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
