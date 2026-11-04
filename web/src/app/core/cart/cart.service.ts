import { Injectable, computed, signal } from '@angular/core';
import { ProductResponse } from '../products/products.models';

export interface CartLine {
  product: ProductResponse;
  quantity: number;
}

// Shared cart state, so the product list and the cart page read the same lines.
@Injectable({ providedIn: 'root' })
export class CartService {
  readonly lines = signal<CartLine[]>([]);

  readonly itemCount = computed(() => this.lines().reduce((count, line) => count + line.quantity, 0));

  readonly total = computed(() => this.lines().reduce((sum, line) => sum + line.product.price * line.quantity, 0));

  quantityOf(productId: string): number {
    return this.lines().find((line) => line.product.id === productId)?.quantity ?? 0;
  }

  add(product: ProductResponse, quantity: number): void {
    this.lines.update((lines) => {
      const existing = lines.find((line) => line.product.id === product.id);
      if (!existing) {
        return [...lines, { product, quantity }];
      }
      return lines.map((line) =>
        line.product.id === product.id ? { ...line, quantity: line.quantity + quantity } : line
      );
    });
  }

  remove(product: ProductResponse, quantity: number): void {
    this.lines.update((lines) => {
      const existing = lines.find((line) => line.product.id === product.id);
      if (!existing || existing.quantity <= quantity) {
        return lines.filter((line) => line.product.id !== product.id);
      }
      return lines.map((line) =>
        line.product.id === product.id ? { ...line, quantity: line.quantity - quantity } : line
      );
    });
  }

  clear(): void {
    this.lines.set([]);
  }
}
