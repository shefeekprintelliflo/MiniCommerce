import { DecimalPipe, NgIf } from '@angular/common';
import { Component, input, output, signal } from '@angular/core';
import { Card } from 'primeng/card';
import { ProductResponse } from '../../core/products/products.models';

@Component({
  selector: 'app-product-card',
  imports: [NgIf, DecimalPipe, Card],
  templateUrl: './product-card.component.html',
  styleUrl: './product-card.component.css',
})
export class ProductCardComponent {
  // Parent -> child: the product to display.
  readonly product = input.required<ProductResponse>();

  // Parent -> child: how many of this product the shopper already has in the cart. 0 hides the badge.
  readonly inCart = input(0);

  // Child -> parent: fired when the shopper presses "Add to cart". The parent decides what that means.
  readonly addToCart = output<{ product: ProductResponse; quantity: number }>();

  readonly removeFromCart = output<{ product: ProductResponse; quantity: number }>();

  protected onRemove(): void {
    this.removeFromCart.emit({ product: this.product(), quantity: this.quantity() });
  }

  // The quantity the shopper is typing. Separate from inCart, which is what is already in the cart.
  protected readonly quantity = signal(1);

  protected onQuantityInput(raw: string): void {
    const whole = Math.floor(Number(raw));
    this.quantity.set(Number.isFinite(whole) && whole >= 1 ? whole : 1);
  }

  protected onAdd(): void {
    this.addToCart.emit({ product: this.product(), quantity: this.quantity() });
  }
}
