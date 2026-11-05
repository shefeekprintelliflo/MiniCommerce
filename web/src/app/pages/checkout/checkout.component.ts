import { HttpErrorResponse } from '@angular/common/http';
import { DecimalPipe, NgFor, NgIf } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CartService } from '../../core/cart/cart.service';
import { OrderResponse } from '../../core/orders/orders.models';
import { OrdersService } from '../../core/orders/orders.service';
import { HeaderComponent } from '../../shared/header/header.component';

@Component({
  selector: 'app-checkout',
  imports: [NgIf, NgFor, RouterLink, DecimalPipe, HeaderComponent],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.css',
})
export class CheckoutComponent {
  // Public so the checkoutLeaveGuard can read the item count before the page is left.
  readonly cart = inject(CartService);
  private readonly orders = inject(OrdersService);

  protected readonly placing = signal(false);
  protected readonly placedOrder = signal<OrderResponse | null>(null);
  protected readonly error = signal<string | null>(null);

  protected placeOrder(): void {
    this.placing.set(true);
    this.error.set(null);

    this.orders.placeOrder().subscribe({
      next: (order) => {
        this.placing.set(false);
        this.placedOrder.set(order);
        // The server empties the cart once the order is saved. Reload so the page shows that.
        this.cart.refresh();
      },
      error: (error: unknown) => {
        this.placing.set(false);
        this.error.set(this.messageFrom(error));
        // The server may have changed the cart while checking out, so show what it holds now.
        this.cart.refresh();
      },
    });
  }

  private messageFrom(error: unknown): string {
    // The orders service sends a 409 with { message } when stock or the cart does not allow the order.
    if (error instanceof HttpErrorResponse && error.status === 409) {
      const message = (error.error as { message?: string } | null)?.message;
      if (message) {
        return message;
      }
    }
    return 'Your order could not be placed. Please try again.';
  }
}
