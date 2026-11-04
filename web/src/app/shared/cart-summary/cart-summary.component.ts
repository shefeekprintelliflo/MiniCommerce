import { DecimalPipe, NgFor, NgIf } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { CartLine } from '../../core/cart/cart.service';

@Component({
  selector: 'app-cart-summary',
  imports: [NgIf, NgFor, DecimalPipe],
  templateUrl: './cart-summary.component.html',
  styleUrl: './cart-summary.component.css',
})
export class CartSummaryComponent {
  // Parent -> child: the cart lines to list, owned by the parent.
  readonly lines = input<CartLine[]>([]);

  // Parent -> child: totals computed by the parent, which owns the cart.
  readonly itemCount = input(0);
  readonly total = input(0);

  // Child -> parent: the shopper asked to empty the cart.
  readonly clear = output<void>();
}
