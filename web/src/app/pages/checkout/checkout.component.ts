import { DecimalPipe, NgIf } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CartService } from '../../core/cart/cart.service';

@Component({
  selector: 'app-checkout',
  imports: [NgIf, RouterLink, DecimalPipe],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.css',
})
export class CheckoutComponent {
  // Public so the checkoutLeaveGuard can read the item count before the page is left.
  readonly cart = inject(CartService);
}
